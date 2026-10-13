using System;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.TotalNutrition;
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

        var response = await new InsightsService(repository.Object).GetTotalNutritionAsync(userId, from, to);

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

        var response = await new InsightsService(repository.Object)
            .GetTotalNutritionAsync(Guid.NewGuid(), to.AddDays(-7), to);

        Assert.That(response, Is.EqualTo(new TotalNutritionResponse()));
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
