using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.TotalNutrition;
using CgmLink.Api.Endpoints.Insights.AverageNutrition;
using CgmLink.Api.Endpoints.Insights.MealGlucose;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Api.Services;

public sealed class InsightsService : IInsightsService
{
    private readonly IRepository<Treatment> _treatmentsRepository;
    private readonly IRepository<Reading> _readingsRepository;

    public InsightsService(IRepository<Treatment> treatmentsRepository, IRepository<Reading> readingsRepository)
    {
        _treatmentsRepository = treatmentsRepository;
        _readingsRepository = readingsRepository;
    }

    public async Task<TotalNutritionResponse> GetTotalNutritionAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var totals = await _treatmentsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Where(t => t.UserId == userId && t.Deleted == null && t.Created >= from && t.Created <= to)
            .GroupBy(t => 1)
            .Select(group => new TotalNutritionResponse
            {
                TotalCalories = group.Sum(t => t.Calories),
                TotalCarbs = group.Sum(t => t.Carbs),
                TotalProtein = group.Sum(t => t.Protein),
                TotalFat = group.Sum(t => t.Fat),
            })
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        return totals ?? new TotalNutritionResponse();
    }

    public async Task<AverageNutritionResponse> GetAverageNutritionAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        var nutrition = await _treatmentsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Where(t => t.UserId == userId && t.Deleted == null && t.Created >= from && t.Created <= to)
            .Where(t => t.Meals.Any() || t.Ingredients.Any() || t.NutritionIngredients.Any())
            .Select(t => new { t.Created, t.Calories, t.Carbs, t.Protein, t.Fat })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var dailyNutrition = nutrition.GroupBy(t => t.Created.ToOffset(to.Offset).Date)
            .Select(group => new AverageNutritionResponse
            {
                Calories = group.Sum(t => t.Calories),
                Carbs = group.Sum(t => t.Carbs),
                Protein = group.Sum(t => t.Protein),
                Fat = group.Sum(t => t.Fat),
            })
            .ToList();

        return dailyNutrition.Count == 0 ? new AverageNutritionResponse() : new AverageNutritionResponse
        {
            Calories = dailyNutrition.Average(day => day.Calories),
            Carbs = dailyNutrition.Average(day => day.Carbs),
            Protein = dailyNutrition.Average(day => day.Protein),
            Fat = dailyNutrition.Average(day => day.Fat),
        };
    }

    public async Task<List<MealGlucoseResponse>> GetMealGlucoseAsync(
        Guid userId, DateTimeOffset from, DateTimeOffset to, int intervalMinutes = MealGlucoseConstants.DefaultIntervalMinutes,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (intervalMinutes < MealGlucoseConstants.MinimumIntervalMinutes || intervalMinutes > (to - from).TotalMinutes)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalMinutes));
        }
        var treatments = await _treatmentsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Where(t => t.UserId == userId && t.Deleted == null && t.Created >= from && t.Created <= to)
            .Where(t => t.Meals.Any() || t.Ingredients.Any() || t.NutritionIngredients.Any())
            .Where(t => t.Injection != null && t.Injection.UserId == userId &&
                t.Injection.Insulin != null && t.Injection.Insulin.Type == InsulinType.Bolus &&
                (t.Injection.Insulin.UserId == null || t.Injection.Insulin.UserId == userId))
            .OrderBy(t => t.Created)
            .ThenBy(t => t.Id)
            .Select(t => new { t.Created, t.Injection!.InsulinId, InsulinName = t.Injection.Insulin!.Name })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (treatments.Count == 0)
        {
            return [];
        }

        var readingsFrom = treatments[0].Created.ToUniversalTime().AddMinutes(-MealGlucoseConstants.BaselineMinutes);
        var readingsTo = treatments[^1].Created.ToUniversalTime()
            .AddMinutes(MealGlucoseConstants.WindowMinutes + MealGlucoseConstants.ToleranceMinutes);
        var readings = await _readingsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Where(r => r.UserId == userId && r.Created >= readingsFrom && r.Created <= readingsTo)
            .OrderBy(r => r.Created).ThenBy(r => r.Id)
            .Select(r => new GlucoseSample(r.Created, r.GlucoseLevel))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var series = new List<MealGlucoseResponse>();
        foreach (var group in treatments.GroupBy(t => t.InsulinId))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var points = CalculateMealGlucosePoints(group.Select(t => t.Created), readings, intervalMinutes, cancellationToken);
            if (points.Count == 0)
            {
                continue;
            }

            series.Add(new MealGlucoseResponse
            {
                InsulinId = group.Key,
                InsulinName = group.First().InsulinName,
                Points = points,
            });
        }

        return series.OrderBy(s => s.InsulinName).ThenBy(s => s.InsulinId).ToList();
    }

    private static List<MealGlucosePointResponse> CalculateMealGlucosePoints(
        IEnumerable<DateTimeOffset> treatmentTimes, List<GlucoseSample> readings, int intervalMinutes,
        CancellationToken cancellationToken)
    {
        var eligible = treatmentTimes.Select(created => new
        {
            Created = created,
            Baseline = FindReading(readings, created, MealGlucoseConstants.BaselineMinutes, beforeOnly: true),
        })
            .Where(t => t.Baseline is not null).ToList();
        var points = new List<MealGlucosePointResponse>();
        if (eligible.Count == 0)
        {
            return points;
        }

        for (var minute = 0; minute <= MealGlucoseConstants.WindowMinutes; minute += intervalMinutes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var levels = eligible
                .Select(t => minute == 0 ? t.Baseline : FindReading(readings,
                    t.Created.ToUniversalTime().AddMinutes(minute), MealGlucoseConstants.ToleranceMinutes))
                .Where(reading => reading is not null)
                .Select(reading => reading!.GlucoseLevel)
                .Order()
                .ToList();

            points.Add(new MealGlucosePointResponse
            {
                MinutesAfterFood = minute,
                AverageGlucoseLevel = levels.Count == 0 ? null : levels.Average(),
                P25GlucoseLevel = Percentile(levels, 0.25),
                P75GlucoseLevel = Percentile(levels, 0.75),
            });
        }

        return points;
    }

    private static GlucoseSample? FindReading(
        List<GlucoseSample> readings, DateTimeOffset target, int toleranceMinutes, bool beforeOnly = false)
    {
        var low = 0;
        var high = readings.Count;
        while (low < high)
        {
            var middle = low + (high - low) / 2;
            if (readings[middle].Created <= target)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        var before = low > 0 ? readings[low - 1] : null;
        var after = !beforeOnly && low < readings.Count ? readings[low] : null;
        var nearest = before is null ? after : after is null || target - before.Created <= after.Created - target
            ? before : after;
        return nearest is not null && (nearest.Created - target).Duration() <= TimeSpan.FromMinutes(toleranceMinutes)
            ? nearest : null;
    }

    private static double? Percentile(List<double> sortedValues, double fraction)
    {
        if (sortedValues.Count == 0)
        {
            return null;
        }
        var position = (sortedValues.Count - 1) * fraction;
        var lower = (int)position;
        var upper = (int)Math.Ceiling(position);
        return sortedValues[lower] + (sortedValues[upper] - sortedValues[lower]) * (position - lower);
    }

    private sealed record GlucoseSample(DateTimeOffset Created, double GlucoseLevel);
}
