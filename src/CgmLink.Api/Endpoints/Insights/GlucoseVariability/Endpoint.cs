using FluentValidation;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Insights.GlucoseVariability;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GlucoseVariabilityResponse>, ValidationProblem, UnauthorizedHttpResult>>
        HandleAsync(
            [AsParameters] GlucoseVariabilityRequest request,
            [FromServices] IValidator<GlucoseVariabilityRequest> validator,
            [FromServices] ICurrentUser currentUser,
            [FromServices] IRepository<Reading> repository,
            CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        var to = request.To ?? DateTimeOffset.UtcNow;
        var from = request.From ?? to.AddDays(-7);

        var query = """
                    SELECT
                        AVG([GlucoseLevel]) AS Mean,
                        STDEV([GlucoseLevel]) AS StandardDeviation
                    FROM
                        [readings]
                    WHERE
                        [UserId] = {0}
                        AND [Created] BETWEEN {1} AND {2};
                    """;

        var stats = repository.FromSqlRaw<VarianceStats>(query, new FindOptions { IsAsNoTracking = true }, userId, from, to)
            .AsEnumerable()
            .FirstOrDefault();

        var mean = stats?.Mean ?? 0;
        var standardDeviation = stats?.StandardDeviation ?? 0;
        var coefficientOfVariation = mean > 0 ? standardDeviation / mean * 100 : 0;

        var response = new GlucoseVariabilityResponse
        {
            From = from,
            To = to,
            StandardDeviation = standardDeviation,
            CoefficientOfVariation = coefficientOfVariation
        };

        return TypedResults.Ok(response);
    }

    internal record VarianceStats
    {
        public double? Mean { get; init; }
        public double? StandardDeviation { get; init; }
    }
}
