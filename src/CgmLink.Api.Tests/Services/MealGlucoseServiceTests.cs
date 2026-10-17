using System;
using CgmLink.Api.Endpoints.Insights.MealGlucose;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public sealed class MealGlucoseServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTimeOffset _from = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task Averages_Each_Treatment_Once_And_Calculates_Percentiles()
    {
        var insulin = new Insulin { Name = "Bolus", Type = InsulinType.Bolus };
        var meals = Enumerable.Range(0, 4).Select(day => Food(_from.AddDays(day), insulin)).ToList();
        var readings = new List<Reading>();
        for (var i = 0; i < meals.Count; i++)
        {
            readings.Add(Reading(meals[i].Created.AddMinutes(-5), 100 + i * 20));
            readings.Add(Reading(meals[i].Created.AddMinutes(30), 130 + i * 10));
            // Dense readings in one treatment do not give that treatment more weight.
            if (i == 0)
            {
                readings.Add(Reading(meals[i].Created.AddMinutes(29), 500));
                readings.Add(Reading(meals[i].Created.AddMinutes(31), 500));
            }
        }
        var (service, repository) = Service(meals, readings);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(7), 30);

        var series = response.Single();
        var point = series.Points.Single(p => p.MinutesAfterFood == 30);
        Assert.Multiple(() =>
        {
            Assert.That(series.InsulinId, Is.EqualTo(insulin.Id));
            Assert.That(series.Points.Select(p => p.MinutesAfterFood), Is.EqualTo(Enumerable.Range(0, 13).Select(i => i * 30)));
            Assert.That(series.Points[0].AverageGlucoseLevel, Is.EqualTo(130));
            Assert.That(point.AverageGlucoseLevel, Is.EqualTo(145));
            Assert.That(point.P25GlucoseLevel, Is.EqualTo(137.5));
            Assert.That(point.P75GlucoseLevel, Is.EqualTo(152.5));
            Assert.That(series.Points[2].AverageGlucoseLevel, Is.Null);
            Assert.That(series.Points[2].P25GlucoseLevel, Is.Null);
        });
        repository.Verify(r => r.GetAll(It.IsAny<FindOptions>()), Times.Once);
    }

    [Test]
    public async Task Excludes_Missing_Follow_Up_From_The_Average_And_Percentiles()
    {
        var a = Food(_from);
        var b = Food(_from.AddDays(1), a.Injection.Insulin);
        var (service, _) = Service([a, b], [
            Reading(a.Created, 100), Reading(a.Created.AddMinutes(30), 80), Reading(b.Created, 300),
        ]);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(7), 30);

        var series = response.Single();
        var point = series.Points[1];
        Assert.Multiple(() =>
        {
            Assert.That(series.Points[0].AverageGlucoseLevel, Is.EqualTo(200));
            Assert.That(point.AverageGlucoseLevel, Is.EqualTo(80));
            Assert.That(point.P25GlucoseLevel, Is.EqualTo(80));
        });
    }

    [Test]
    public async Task Filters_Users_Deleted_Treatments_Basal_And_No_Food_And_Groups_By_Id()
    {
        var personal = new Insulin { Name = "Same name", Type = InsulinType.Bolus, UserId = _userId };
        var shared = new Insulin { Name = "Same name", Type = InsulinType.Bolus };
        var meal = Food(_from, personal);
        var external = Food(_from.AddHours(8), shared);
        external.Meals.Clear();
        external.NutritionIngredients.Add(new TreatmentNutritionIngredient { Quantity = 1, Created = external.Created });
        var ingredient = Food(_from.AddHours(12), personal);
        ingredient.Meals.Clear();
        ingredient.Ingredients.Add(new TreatmentIngredient { Quantity = 1, Created = ingredient.Created });
        var deleted = Food(_from);
        deleted.Deleted = _from;
        var otherUser = Food(_from);
        otherUser.UserId = Guid.NewGuid();
        var otherInjection = Food(_from);
        otherInjection.Injection.UserId = Guid.NewGuid();
        var otherInsulin = Food(_from, new Insulin { Name = "Private", Type = InsulinType.Bolus, UserId = Guid.NewGuid() });
        var basal = Food(_from, new Insulin { Name = "Basal", Type = InsulinType.Basal });
        var noFood = Food(_from);
        noFood.Meals.Clear();
        var noInjection = Food(_from);
        noInjection.Injection = null;
        noInjection.InjectionId = null;
        var (service, _) = Service([
            meal, external, ingredient, deleted, otherUser, otherInjection, otherInsulin, basal, noFood, noInjection,
            Food(_from.AddTicks(-1)), Food(_from.AddDays(7).AddTicks(1)),
        ], [Reading(_from, 100), Reading(external.Created, 120), Reading(ingredient.Created, 140),
            Reading(_from, 999, Guid.NewGuid())]);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(7), 30);

        Assert.That(response, Has.Count.EqualTo(2));
        Assert.That(response.Single(s => s.InsulinId == personal.Id).Points[0].AverageGlucoseLevel, Is.EqualTo(120));
        Assert.That(response.Single(s => s.InsulinId == shared.Id).Points[0].AverageGlucoseLevel, Is.EqualTo(120));
    }

    [Test]
    public async Task Includes_Period_Boundaries_And_Loads_Readings_Outside_The_Treatment_Period()
    {
        var first = Food(_from);
        var last = Food(_from.AddDays(7).ToOffset(TimeSpan.FromHours(2)), first.Injection.Insulin);
        var (service, _) = Service([last, first], [
            Reading(_from.AddMinutes(-15), 100), Reading(last.Created, 120),
            Reading(last.Created.AddMinutes(370), 150),
            Reading(last.Created.AddMinutes(370).AddTicks(1), 999),
        ]);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(7), 30);

        var series = response.Single();
        Assert.That(series.Points[0].AverageGlucoseLevel, Is.EqualTo(110));
        Assert.That(series.Points[^1].AverageGlucoseLevel, Is.EqualTo(150));
    }

    [TestCase(-10, 0, 150)]
    [TestCase(10, 0, 150)]
    [TestCase(-10, -1, null)]
    [TestCase(10, 1, null)]
    public async Task Matches_Only_Readings_Within_Inclusive_Tolerance(int minutes, int ticks, double? expected)
    {
        var (service, _) = Service([Food(_from)], [
            Reading(_from, 100), Reading(_from.AddMinutes(30 + minutes).AddTicks(ticks), 150),
        ]);
        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), 30);

        Assert.That(response.Single().Points[1].AverageGlucoseLevel, Is.EqualTo(expected));
    }

    [Test]
    public async Task Selects_Latest_Baseline_And_Earlier_Reading_On_Equal_Distance()
    {
        var (service, _) = Service([Food(_from)], [
            Reading(_from.AddMinutes(35), 180), Reading(_from.AddMinutes(-10), 90),
            Reading(_from.AddMinutes(25), 140), Reading(_from.AddMinutes(-2), 100),
            Reading(_from.AddMinutes(1), 200),
        ]);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), 30);

        Assert.That(response.Single().Points[0].AverageGlucoseLevel, Is.EqualTo(100));
        Assert.That(response.Single().Points[1].AverageGlucoseLevel, Is.EqualTo(140));
    }

    [TestCase(-15, -1)]
    [TestCase(1, 0)]
    public async Task Omits_Treatments_Without_A_Recent_Pre_Food_Baseline(int minutes, int ticks)
    {
        var (service, _) = Service([Food(_from)], [Reading(_from.AddMinutes(minutes).AddTicks(ticks), 100)]);
        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), 30);

        Assert.That(response, Is.Empty);
    }

    [Test]
    public async Task Does_Not_Fetch_Readings_When_No_Treatments_Match()
    {
        var (service, repository) = Service([], []);
        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), 30);

        Assert.That(response, Is.Empty);
        repository.VerifyNoOtherCalls();
    }

    [Test]
    public void Honors_Cancellation()
    {
        var (service, _) = Service([Food(_from)], [Reading(_from, 100)]);
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(async () => await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), 30, source.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [TestCase(5)]
    [TestCase(7)]
    [TestCase(45)]
    [TestCase(360)]
    [TestCase(1440)]
    public async Task Uses_Requested_Interval_For_Point_Times_And_Matching(int intervalMinutes)
    {
        var (service, _) = Service([Food(_from)], [
            Reading(_from, 100), Reading(_from.AddMinutes(intervalMinutes), 180),
        ]);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), intervalMinutes);

        var points = response.Single().Points;
        Assert.That(points.Select(p => p.MinutesAfterFood),
            Is.EqualTo(Enumerable.Range(0, 360 / intervalMinutes + 1).Select(i => i * intervalMinutes)));
        Assert.That(points[0].AverageGlucoseLevel, Is.EqualTo(100));
        if (intervalMinutes <= 360)
        {
            Assert.That(points[1].AverageGlucoseLevel, Is.EqualTo(180));
        }
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(4)]
    [TestCase(1441)]
    public void Rejects_Invalid_Interval_Before_Querying(int intervalMinutes)
    {
        var (service, repository) = Service([], []);

        Assert.That(async () => await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), intervalMinutes),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        repository.VerifyNoOtherCalls();
    }

    [Test]
    public async Task Uses_Configured_Window_Baseline_And_Reading_Tolerance()
    {
        var first = Food(_from);
        var second = Food(_from.AddDays(1), first.Injection.Insulin);
        var settings = new MealGlucoseSettings
        {
            WindowMinutes = 20,
            DefaultIntervalMinutes = 10,
            MinimumIntervalMinutes = 10,
            BaselineMinutes = 3,
            ToleranceMinutes = 1,
        };
        var (service, _) = Service([first, second], [
            Reading(first.Created.AddMinutes(-3), 110),
            Reading(first.Created.AddMinutes(12), 150),
            Reading(first.Created.AddMinutes(21), 160),
            Reading(second.Created.AddMinutes(-4), 999),
        ], settings);

        var response = await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(2), 10);

        var points = response.Single().Points;
        Assert.That(points.Select(p => p.MinutesAfterFood), Is.EqualTo(new[] { 0, 10, 20 }));
        Assert.That(points[0].AverageGlucoseLevel, Is.EqualTo(110));
        Assert.That(points[1].AverageGlucoseLevel, Is.Null);
        Assert.That(points[2].AverageGlucoseLevel, Is.EqualTo(160));
    }

    [Test]
    public void Enforces_Configured_Minimum_Interval()
    {
        var settings = new MealGlucoseSettings
        {
            WindowMinutes = 360,
            DefaultIntervalMinutes = 30,
            MinimumIntervalMinutes = 10,
            BaselineMinutes = 15,
            ToleranceMinutes = 10,
        };
        var (service, repository) = Service([], [], settings);

        Assert.That(async () => await service.GetMealGlucoseAsync(_userId, _from, _from.AddDays(1), 5),
            Throws.TypeOf<ArgumentOutOfRangeException>());
        repository.VerifyNoOtherCalls();
    }

    private (InsightsService Service, Mock<IRepository<Reading>> Readings) Service(
        IEnumerable<Treatment> treatments, IEnumerable<Reading> readings, MealGlucoseSettings settings = null)
    {
        settings ??= new MealGlucoseSettings
        {
            WindowMinutes = 360,
            DefaultIntervalMinutes = 30,
            MinimumIntervalMinutes = 5,
            BaselineMinutes = 15,
            ToleranceMinutes = 10,
        };
        var treatmentRepository = new Mock<IRepository<Treatment>>();
        treatmentRepository.Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(treatments));
        var readingRepository = new Mock<IRepository<Reading>>();
        readingRepository.Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Reading>(readings));
        return (new InsightsService(treatmentRepository.Object, readingRepository.Object, Options.Create(settings)), readingRepository);
    }

    private Treatment Food(DateTimeOffset created, Insulin insulin = null)
    {
        insulin ??= new Insulin { Name = "Bolus", Type = InsulinType.Bolus };
        var injection = new Injection { UserId = _userId, InsulinId = insulin.Id, Insulin = insulin, Units = 4, Created = created };
        return new Treatment
        {
            UserId = _userId,
            Created = created,
            Calories = 100,
            Carbs = 20,
            Protein = 5,
            Fat = 2,
            InjectionId = injection.Id,
            Injection = injection,
            Meals = [new TreatmentMeal { Quantity = 1, Created = created }],
        };
    }

    private Reading Reading(DateTimeOffset created, double glucose, Guid? userId = null) => new()
    {
        UserId = userId ?? _userId,
        Created = created,
        GlucoseLevel = glucose,
        Direction = ReadingDirection.Steady,
    };
}
