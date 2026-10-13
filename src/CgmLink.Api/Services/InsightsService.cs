using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.TotalNutrition;
using CgmLink.Api.Endpoints.Insights.AverageNutrition;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Api.Services;

public sealed class InsightsService : IInsightsService
{
    private readonly IRepository<Treatment> _treatmentsRepository;

    public InsightsService(IRepository<Treatment> treatmentsRepository)
    {
        _treatmentsRepository = treatmentsRepository;
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
            .AsNoTracking()
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
}
