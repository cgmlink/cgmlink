using System;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CgmLink.Api.Endpoints.Insights.AverageNutrition;

internal static class Endpoint
{
    internal static async Task<Results<Ok<AverageNutritionResponse>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        [AsParameters] AverageNutritionRequest request,
        [FromServices] IValidator<AverageNutritionRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IInsightsService insightsService,
        CancellationToken cancellationToken = default)
    {
        var to = request.To ?? DateTimeOffset.UtcNow;
        var from = request.From ?? to.AddDays(-7);
        var resolvedRequest = request with { From = from, To = to };

        var validation = await validator.ValidateAsync(resolvedRequest, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        var average = await insightsService.GetAverageNutritionAsync(userId, from, to, cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(average);
    }
}
