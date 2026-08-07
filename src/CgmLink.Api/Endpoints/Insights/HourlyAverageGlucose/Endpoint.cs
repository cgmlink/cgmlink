using FluentValidation;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Insights.HourlyAverageGlucose;

internal static class Endpoint
{
    internal static async Task<Results<Ok<List<HourlyAverageGlucoseResponse>>, UnauthorizedHttpResult, ValidationProblem>>
        HandleAsync(
            [AsParameters] HourlyAverageGlucoseRequest request,
            [FromServices] IValidator<HourlyAverageGlucoseRequest> validator,
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
        var offsetMinutes = (int)to.Offset.TotalMinutes;

        var query = """
                    SELECT
                        DATEPART(HOUR, DATEADD(MINUTE, {3}, [Created])) AS Hour,
                        AVG([GlucoseLevel]) AS AverageGlucoseLevel,
                        COUNT(*) AS ReadingCount
                    FROM
                        [readings]
                    WHERE
                        [UserId] = {0}
                        AND [Created] BETWEEN {1} AND {2}
                    GROUP BY
                        DATEPART(HOUR, DATEADD(MINUTE, {3}, [Created]));
                    """;

        var byHour = repository.FromSqlRaw<HourlyAverage>(
                query, new FindOptions { IsAsNoTracking = true }, userId, from, to, offsetMinutes)
            .AsEnumerable()
            .ToDictionary(r => r.Hour, r => (r.AverageGlucoseLevel, r.ReadingCount));

        var response = Enumerable.Range(0, 24)
            .Select(hour => byHour.TryGetValue(hour, out var bucket)
                ? new HourlyAverageGlucoseResponse { Hour = hour, AverageGlucoseLevel = bucket.AverageGlucoseLevel, ReadingCount = bucket.ReadingCount }
                : new HourlyAverageGlucoseResponse { Hour = hour, AverageGlucoseLevel = null, ReadingCount = 0 })
            .ToList();

        return TypedResults.Ok(response);
    }

    internal record HourlyAverage
    {
        public int Hour { get; init; }
        public double AverageGlucoseLevel { get; init; }
        public int ReadingCount { get; init; }
    }
}
