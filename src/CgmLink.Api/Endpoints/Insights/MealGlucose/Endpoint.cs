using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CgmLink.Api.Endpoints.Insights.MealGlucose;

internal static class Endpoint
{
    internal static async Task<Results<Ok<List<MealGlucoseResponse>>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        [AsParameters] MealGlucoseRequest request,
        [FromServices] IValidator<MealGlucoseRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IInsightsService insightsService,
        [FromServices] IOptions<MealGlucoseSettings> mealGlucoseSettings,
        CancellationToken cancellationToken = default)
    {
        var to = request.To ?? DateTimeOffset.UtcNow;
        var from = request.From ?? (to >= DateTimeOffset.MinValue.AddDays(7)
            ? to.ToUniversalTime().AddDays(-7) : DateTimeOffset.MinValue);
        var intervalMinutes = request.IntervalMinutes ?? mealGlucoseSettings.Value.DefaultIntervalMinutes;
        var validation = await validator.ValidateAsync(request with { From = from, To = to, IntervalMinutes = intervalMinutes }, cancellationToken)
            .ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var response = await insightsService.GetMealGlucoseAsync(currentUser.GetUserId(), from, to, intervalMinutes, cancellationToken)
            .ConfigureAwait(false);
        return TypedResults.Ok(response);
    }
}
