using System;
using CgmLink.Api.Endpoints.Insights.MealGlucose;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.TotalNutrition;
using CgmLink.Api.Endpoints.Insights.AverageNutrition;
using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public sealed class InsightsServiceTests
{
    [Test]
    public async Task GetTotalNutritionAsync_Sums_Stored_Totals_Within_Inclusive_Period_For_Current_User()
    {
        var userId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(7);
        var deleted = CreateTreatment(userId, from.AddDays(1), 100m);
        deleted.Deleted = to;
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>([
            CreateTreatment(userId, from, 100.25m),
            CreateTreatment(userId, from.AddDays(2), 200.50m),
            CreateTreatment(userId, to.ToOffset(TimeSpan.FromHours(1)), 300.75m),
            CreateTreatment(userId, from.AddDays(3), 0m),
            CreateTreatment(userId, from.AddTicks(-1), 1000m),
            CreateTreatment(userId, to.AddTicks(1), 1000m),
            CreateTreatment(Guid.NewGuid(), from, 1000m),
            deleted,
        ]));

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings())).GetTotalNutritionAsync(userId, from, to);

        Assert.That(response, Is.EqualTo(new TotalNutritionResponse
        {
            TotalCalories = 601.50m,
            TotalCarbs = 60.150m,
            TotalProtein = 30.075m,
            TotalFat = 12.030m,
        }));
    }

    [Test]
    public async Task GetTotalNutritionAsync_Returns_Zero_When_No_Treatments_Match()
    {
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>([]));
        var to = DateTimeOffset.UtcNow;

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings()))
            .GetTotalNutritionAsync(Guid.NewGuid(), to.AddDays(-7), to);

        Assert.That(response, Is.EqualTo(new TotalNutritionResponse()));
    }

    [Test]
    public async Task GetAverageNutritionAsync_Averages_Daily_Snapshot_Totals_Only_For_Days_With_Food()
    {
        var userId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(7);
        var deleted = CreateFoodTreatment(userId, from.AddDays(3), 1000m, "meal");
        deleted.Deleted = to;
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>([
            CreateFoodTreatment(userId, from.AddHours(8), 100m, "personal"),
            CreateFoodTreatment(userId, from.AddHours(12), 200m, "meal"),
            CreateFoodTreatment(userId, from.AddDays(4), 500m, "external"),
            CreateTreatment(userId, from.AddDays(1), 0m),
            CreateFoodTreatment(Guid.NewGuid(), from.AddDays(2), 1000m, "personal"),
            deleted,
        ]));

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings())).GetAverageNutritionAsync(userId, from, to);

        // Two recorded days: (100 + 200 + 500) / 2, not three treatments or seven days.
        Assert.That(response, Is.EqualTo(new AverageNutritionResponse
        {
            Calories = 400m,
            Carbs = 40m,
            Protein = 20m,
            Fat = 8m,
        }));
    }

    [Test]
    public async Task GetAverageNutritionAsync_Includes_Days_With_Zero_Nutrition_Food()
    {
        var userId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>([
            CreateFoodTreatment(userId, from, 100m, "personal"),
            CreateFoodTreatment(userId, from.AddDays(1), 0m, "external"),
            CreateTreatment(userId, from.AddDays(2), 0m),
        ]));

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings())).GetAverageNutritionAsync(userId, from, from.AddDays(7));

        Assert.That(response.Calories, Is.EqualTo(50m));
    }

    [Test]
    public async Task GetAverageNutritionAsync_Normalizes_Treatment_Offsets_To_To_Offset_For_Days()
    {
        var userId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(7).ToOffset(TimeSpan.FromHours(1));
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>([
            CreateFoodTreatment(userId, from.AddHours(22.5), 100m, "personal"),
            CreateFoodTreatment(userId, from.AddHours(22.75).ToOffset(TimeSpan.FromHours(2)), 200m, "meal"),
            CreateFoodTreatment(userId, from.AddHours(23.5), 500m, "external"),
        ]));

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings())).GetAverageNutritionAsync(userId, from, to);

        Assert.That(response.Calories, Is.EqualTo(400m));
    }

    [Test]
    public async Task GetAverageNutritionAsync_Includes_Boundaries_And_Excludes_Outside_Period()
    {
        var userId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddDays(7);
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>([
            CreateFoodTreatment(userId, from, 100m, "personal"),
            CreateFoodTreatment(userId, to, 200m, "external"),
            CreateFoodTreatment(userId, from.AddTicks(-1), 1000m, "meal"),
            CreateFoodTreatment(userId, to.AddTicks(1), 1000m, "meal"),
        ]));

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings())).GetAverageNutritionAsync(userId, from, to);

        Assert.That(response.Calories, Is.EqualTo(150m));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task GetAverageNutritionAsync_Returns_Zero_When_No_Food_Is_Recorded(bool hasTreatment)
    {
        var userId = Guid.NewGuid();
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var repository = new Mock<IRepository<Treatment>>();
        repository.Setup(r => r.GetAll(It.IsAny<FindOptions>())).Returns(new TestAsyncEnumerable<Treatment>(
            hasTreatment ? [CreateTreatment(userId, from, 0m)] : []));

        var response = await new InsightsService(repository.Object, Mock.Of<IRepository<Reading>>(), Options.Create(new MealGlucoseSettings())).GetAverageNutritionAsync(userId, from, from.AddDays(7));

        Assert.That(response, Is.EqualTo(new AverageNutritionResponse()));
    }

    private static Treatment CreateFoodTreatment(Guid userId, DateTimeOffset created, decimal calories, string source)
    {
        var treatment = CreateTreatment(userId, created, calories);
        switch (source)
        {
            case "meal":
                treatment.Meals.Add(new TreatmentMeal { MealId = Guid.NewGuid(), Quantity = 1m, Created = created });
                break;
            case "personal":
                treatment.Ingredients.Add(new TreatmentIngredient { IngredientId = Guid.NewGuid(), Quantity = 1m, Created = created });
                break;
            case "external":
                treatment.NutritionIngredients.Add(new TreatmentNutritionIngredient { NutritionIngredientId = Guid.NewGuid(), Quantity = 1m, Created = created });
                break;
        }
        return treatment;
    }

    private static Treatment CreateTreatment(Guid userId, DateTimeOffset created, decimal calories) => new()
    {
        UserId = userId,
        Created = created,
        Calories = calories,
        Carbs = calories / 10m,
        Protein = calories / 20m,
        Fat = calories / 50m,
    };
}
