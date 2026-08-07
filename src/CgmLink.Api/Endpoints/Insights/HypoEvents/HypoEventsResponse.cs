using System;

namespace CgmLink.Api.Endpoints.Insights.HypoEvents;

public record HypoEventsResponse
{
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public required int HypoEventCount { get; init; }
    public required int LongestStreakInRangeMinutes { get; init; }
}
