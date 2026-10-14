using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Insights.MealGlucose;
using CgmLink.Api.Services;
using CgmLink.Api.Middleware;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Insights;

[TestFixture]
public sealed class MealGlucoseEndpointTests
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
        _service.Setup(s => s.GetMealGlucoseAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<DateTimeOffset>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<MealGlucoseResponse>());
    }

    [Test]
    public async Task HandleAsync_Returns_Service_Averages_And_Forwards_Period_User_And_Cancellation()
    {
        var to = DateTimeOffset.UtcNow;
        var from = to.AddDays(-14);
        var totals = new List<MealGlucoseResponse> { new() { InsulinId = Guid.NewGuid(), InsulinName = "Bolus" } };
        using var cancellation = new CancellationTokenSource();
        _service.Setup(s => s.GetMealGlucoseAsync(_userId, from, to, 30, cancellation.Token)).ReturnsAsync(totals);

        var result = await Endpoint.HandleAsync(new MealGlucoseRequest { From = from, To = to },
            new MealGlucoseRequest.Validator(), _currentUser.Object, _service.Object, cancellation.Token);

        Assert.That(result.Result, Is.TypeOf<Ok<List<MealGlucoseResponse>>>());
        Assert.That(((Ok<List<MealGlucoseResponse>>)result.Result).Value, Is.SameAs(totals));
        _service.Verify(s => s.GetMealGlucoseAsync(_userId, from, to, 30, cancellation.Token), Times.Once);
    }

    [Test]
    public async Task HandleAsync_Defaults_To_Seven_Days_Ending_Now()
    {
        var before = DateTimeOffset.UtcNow;
        await HandleAsync(new MealGlucoseRequest());
        var after = DateTimeOffset.UtcNow;

        _service.Verify(s => s.GetMealGlucoseAsync(_userId,
            It.Is<DateTimeOffset>(from => from >= before.AddDays(-7) && from <= after.AddDays(-7)),
            It.Is<DateTimeOffset>(to => to >= before && to <= after), 30, CancellationToken.None), Times.Once);
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(4)]
    [TestCase(11)]
    public async Task HandleAsync_Rejects_Intervals_Outside_Minimum_And_Period(int intervalMinutes)
    {
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await HandleAsync(new MealGlucoseRequest
        {
            From = from,
            To = from.AddMinutes(10),
            IntervalMinutes = intervalMinutes,
        });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        Assert.That(((ValidationProblem)result.Result).ProblemDetails.Errors, Contains.Key("IntervalMinutes"));
        Assert.That(((ValidationProblem)result.Result).ProblemDetails.Errors["IntervalMinutes"],
            Is.All.EqualTo(Resources.ValidationMessages.IntervalInvalid));
        _service.VerifyNoOtherCalls();
    }

    [TestCase(5)]
    [TestCase(10)]
    public async Task HandleAsync_Accepts_Minimum_And_Maximum_And_Forwards_Interval(int intervalMinutes)
    {
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var to = from.AddMinutes(10).ToOffset(TimeSpan.FromHours(1));
        var result = await HandleAsync(new MealGlucoseRequest { From = from, To = to, IntervalMinutes = intervalMinutes });

        Assert.That(result.Result, Is.TypeOf<Ok<List<MealGlucoseResponse>>>());
        _service.Verify(s => s.GetMealGlucoseAsync(_userId, from, to, intervalMinutes, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task HandleAsync_Does_Not_Round_Up_The_Maximum_Interval()
    {
        var from = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var result = await HandleAsync(new MealGlucoseRequest
        {
            From = from,
            To = from.AddMinutes(5).AddTicks(-1),
            IntervalMinutes = 5,
        });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_Validates_Interval_Against_Defaulted_Dates()
    {
        var result = await HandleAsync(new MealGlucoseRequest { IntervalMinutes = 7 * 24 * 60 + 1 });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_With_Only_To_Defaults_From_To_Seven_Days_Earlier()
    {
        var to = new DateTimeOffset(2026, 10, 8, 12, 0, 0, TimeSpan.FromHours(1));
        await HandleAsync(new MealGlucoseRequest { To = to });

        _service.Verify(s => s.GetMealGlucoseAsync(_userId, to.AddDays(-7), to, 30, CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task HandleAsync_With_Only_From_Defaults_To_Now()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-1);
        var before = DateTimeOffset.UtcNow;
        await HandleAsync(new MealGlucoseRequest { From = from });
        var after = DateTimeOffset.UtcNow;

        _service.Verify(s => s.GetMealGlucoseAsync(_userId, from,
            It.Is<DateTimeOffset>(to => to >= before && to <= after), 30, CancellationToken.None), Times.Once);
    }

    [TestCase(0)]
    [TestCase(1)]
    public async Task HandleAsync_Rejects_Equal_Or_Reversed_Dates(int daysAfterTo)
    {
        var to = DateTimeOffset.UtcNow;
        var result = await HandleAsync(new MealGlucoseRequest { From = to.AddDays(daysAfterTo), To = to });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_Rejects_Future_From_When_To_Is_Omitted()
    {
        var result = await HandleAsync(new MealGlucoseRequest { From = DateTimeOffset.UtcNow.AddDays(1) });

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public void HandleAsync_Does_Not_Call_Service_When_User_Is_Unauthorized()
    {
        _currentUser.Setup(u => u.GetUserId()).Throws<UnauthorizedAccessException>();

        Assert.That(async () => await HandleAsync(new MealGlucoseRequest()), Throws.TypeOf<UnauthorizedAccessException>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public async Task HandleAsync_Rejects_Date_Extremes_Without_Overflow()
    {
        var minimum = await HandleAsync(new MealGlucoseRequest { To = DateTimeOffset.MinValue });
        var maximum = await HandleAsync(new MealGlucoseRequest
        {
            From = DateTimeOffset.UtcNow,
            To = DateTimeOffset.MaxValue,
        });

        Assert.That(minimum.Result, Is.TypeOf<ValidationProblem>());
        Assert.That(maximum.Result, Is.TypeOf<ValidationProblem>());
        _service.VerifyNoOtherCalls();
    }

    [Test]
    public void Response_Contains_Only_Insulin_And_Graph_Points()
    {
        var response = new List<MealGlucoseResponse>
        {
            new()
            {
                InsulinId = Guid.NewGuid(), InsulinName = "Bolus",
                Points = [new MealGlucosePointResponse { MinutesAfterFood = 60 }],
            },
        };
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var series = json.RootElement[0];
        var point = series.GetProperty("points")[0];

        Assert.That(json.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array));
        Assert.That(series.EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "insulinId", "insulinName", "points" }));
        Assert.That(point.EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[]
        {
            "minutesAfterFood", "averageGlucoseLevel", "p25GlucoseLevel", "p75GlucoseLevel",
        }));
        Assert.That(point.GetProperty("averageGlucoseLevel").ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [TestCase(null, HttpStatusCode.OK, 30)]
    [TestCase("5", HttpStatusCode.OK, 5)]
    [TestCase("not-a-number", HttpStatusCode.BadRequest, null)]
    [TestCase("4", HttpStatusCode.BadRequest, null)]
    public async Task Http_Query_Binding_Uses_Default_And_Rejects_Invalid_Intervals(
        string interval, HttpStatusCode expectedStatus, int? expectedInterval)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
        builder.Services.AddSingleton<IInsightsService>(_service.Object);
        builder.Services.AddSingleton<ICurrentUser>(_currentUser.Object);
        builder.Services.AddSingleton<IValidator<MealGlucoseRequest>>(new MealGlucoseRequest.Validator());
        builder.Services.AddTransient<ExceptionMiddleware>();
        await using var app = builder.Build();
        app.UseMiddleware<ExceptionMiddleware>();
        Delegate handler = Endpoint.HandleAsync;
        app.MapGet("/meal-glucose", handler);
        await app.StartAsync();

        try
        {
            var address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>().Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address) };
            var query = interval is null ? "" : "?intervalMinutes=" + Uri.EscapeDataString(interval);
            using var response = await client.GetAsync("/meal-glucose" + query);

            Assert.That(response.StatusCode, Is.EqualTo(expectedStatus));
            if (expectedInterval.HasValue)
            {
                _service.Verify(s => s.GetMealGlucoseAsync(_userId, It.IsAny<DateTimeOffset>(),
                    It.IsAny<DateTimeOffset>(), expectedInterval.Value, It.IsAny<CancellationToken>()), Times.Once);
            }
            else
            {
                _service.VerifyNoOtherCalls();
            }
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private Task<Results<Ok<List<MealGlucoseResponse>>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        MealGlucoseRequest request) => Endpoint.HandleAsync(request, new MealGlucoseRequest.Validator(),
            _currentUser.Object, _service.Object);
}
