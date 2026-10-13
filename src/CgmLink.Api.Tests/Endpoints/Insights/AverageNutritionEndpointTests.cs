using System;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.AverageNutrition;
using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Insights;

[TestFixture]
public sealed class AverageNutritionEndpointTests
{
    private Mock<ICurrentUser> _currentUser;
    private Mock<IInsightsService> _service;
    private Guid _userId;

    [SetUp]
    public void SetUp()
    {
        _userId = Guid.NewGuid();
        _currentUser = new Mock<ICurrentUser>();
        _currentUser.Setup(u => u.GetUserId()).Returns(_userId);
        _service = new Mock<IInsightsService>();
        _service.Setup(s => s.GetAverageNutritionAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(new AverageNutritionResponse());
    }

    [Test]
    public async Task HandleAsync_Returns_Service_Averages_And_Forwards_Period_User_And_Cancellation()
    {
        var to = DateTimeOffset.UtcNow;
        var from = to.AddDays(-14);
        var totals = new AverageNutritionResponse { Calories = 150.25m, Carbs = 12m, Protein = 8m, Fat = 5m };
        using var cancellation = new CancellationTokenSource();
        _service.Setup(s => s.GetAverageNutritionAsync(_userId, from, to, cancellation.Token)).ReturnsAsync(totals);

        var result = await Endpoint.HandleAsync(new AverageNutritionRequest { From = from, To = to },
            new AverageNutritionRequest.Validator(), _currentUser.Object, _service.Object, cancellation.Token);

        Assert.That(result.Result, Is.TypeOf<Ok<AverageNutritionResponse>>());
        Assert.That(((Ok<AverageNutritionResponse>)result.Result).Value, Is.SameAs(totals));
        _service.Verify(s => s.GetAverageNutritionAsync(_userId, from, to, cancellation.Token), Times.Once);
    }

    [Test]
    public async Task HandleAsync_Defaults_To_Seven_Days_Ending_Now()
    {
        var before = DateTimeOffset.UtcNow;
        await HandleAsync(new AverageNutritionRequest());
        var after = DateTimeOffset.UtcNow;

        _service.Verify(s => s.GetAverageNutritionAsync(_userId,
            It.Is<DateTimeOffset>(from => from >= before.AddDays(-7) && from <= after.AddDays(-7)),
            It.Is<DateTimeOffset>(to => to >= before && to <= after), CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task HandleAsync_With_Only_To_Defaults_From_To_Seven_Days_Earlier()
    {
        var to = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(1));
        await HandleAsync(new AverageNutritionRequest { To = to });

        _service.Verify(s => s.GetAverageNutritionAsync(_userId, to.AddDays(-7), to, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task HandleAsync_With_Only_From_Defaults_To_Now()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var before = DateTimeOffset.UtcNow;
        await HandleAsync(new AverageNutritionRequest { From = from });
        var after = DateTimeOffset.UtcNow;

        _service.Verify(s => s.GetAverageNutritionAsync(_userId, from,
            It.Is<DateTimeOffset>(to => to >= before && to <= after), CancellationToken.None), Times.Once);
    }

    [TestCase(0)]
    [TestCase(1)]
    public async Task HandleAsync_Rejects_Equal_Or_Reversed_Dates(int daysAfterTo)
    {
        var to = DateTimeOffset.UtcNow;
        var result = await HandleAsync(new AverageNutritionRequest { From = to.AddDays(daysAfterTo), To = to });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_Rejects_Future_From_When_To_Is_Omitted()
    {
        var result = await HandleAsync(new AverageNutritionRequest { From = DateTimeOffset.UtcNow.AddDays(1) });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_Does_Not_Call_Service_When_User_Is_Unauthorized()
    {
        _currentUser.Setup(u => u.GetUserId()).Throws<UnauthorizedAccessException>();

        Assert.That(async () => await HandleAsync(new AverageNutritionRequest()), Throws.TypeOf<UnauthorizedAccessException>());
        _service.VerifyNoOtherCalls();
    }

    private Task<Results<Ok<AverageNutritionResponse>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        AverageNutritionRequest request) => Endpoint.HandleAsync(request, new AverageNutritionRequest.Validator(),
            _currentUser.Object, _service.Object);
}
