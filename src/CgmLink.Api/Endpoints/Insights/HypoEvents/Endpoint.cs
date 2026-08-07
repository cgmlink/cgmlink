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

namespace CgmLink.Api.Endpoints.Insights.HypoEvents;

internal static class Endpoint
{
    internal static async Task<Results<Ok<HypoEventsResponse>, ValidationProblem, UnauthorizedHttpResult>>
        HandleAsync(
            [AsParameters] HypoEventsRequest request,
            [FromServices] IValidator<HypoEventsRequest> validator,
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
                    WITH UserTargets AS (
                        SELECT LowSugarThreshold, HighSugarThreshold
                        FROM user_settings
                        WHERE UserId = {0}
                    ),
                    TimeRanges AS (
                        SELECT
                            r.Created AS StartTime,
                            LEAD(r.Created) OVER (ORDER BY r.Created) AS EndTime,
                            r.GlucoseLevel,
                            ut.LowSugarThreshold,
                            ut.HighSugarThreshold
                        FROM readings r
                        CROSS JOIN UserTargets ut
                        WHERE r.UserId = {0} AND r.Created BETWEEN {1} AND {2}
                    ),
                    Classified AS (
                        SELECT
                            StartTime,
                            DATEDIFF(minute, StartTime, EndTime) AS DurationMinutes,
                            CASE
                                WHEN GlucoseLevel < LowSugarThreshold THEN 0
                                WHEN GlucoseLevel BETWEEN LowSugarThreshold AND HighSugarThreshold THEN 1
                                ELSE 2
                            END AS RangeGroup
                        FROM TimeRanges
                        WHERE EndTime IS NOT NULL
                    ),
                    Grouped AS (
                        SELECT
                            *,
                            ROW_NUMBER() OVER (ORDER BY StartTime) - ROW_NUMBER() OVER (PARTITION BY RangeGroup ORDER BY StartTime) AS StreakKey
                        FROM Classified
                    ),
                    Episodes AS (
                        SELECT
                            RangeGroup,
                            SUM(DurationMinutes) AS TotalMinutes
                        FROM Grouped
                        GROUP BY RangeGroup, StreakKey
                    )
                    SELECT
                        (SELECT COUNT(*) FROM Episodes WHERE RangeGroup = 0) AS HypoEventCount,
                        (SELECT COALESCE(MAX(TotalMinutes), 0) FROM Episodes WHERE RangeGroup = 1) AS LongestStreakInRangeMinutes;
                    """;

        var stats = repository.FromSqlRaw<HypoStreakStats>(query, new FindOptions { IsAsNoTracking = true }, userId, from, to)
            .AsEnumerable()
            .FirstOrDefault();

        var response = new HypoEventsResponse
        {
            From = from,
            To = to,
            HypoEventCount = stats?.HypoEventCount ?? 0,
            LongestStreakInRangeMinutes = stats?.LongestStreakInRangeMinutes ?? 0
        };

        return TypedResults.Ok(response);
    }

    internal record HypoStreakStats
    {
        public int HypoEventCount { get; init; }
        public int LongestStreakInRangeMinutes { get; init; }
    }
}
