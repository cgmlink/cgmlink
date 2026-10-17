using System;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Insights.MealGlucose;

public sealed record MealGlucoseResponse
{
    public Guid InsulinId { get; init; }
    public required string InsulinName { get; init; }
    public IReadOnlyList<MealGlucosePointResponse> Points { get; init; } = [];
}

public sealed record MealGlucosePointResponse
{
    public int MinutesAfterFood { get; init; }
    public double? AverageGlucoseLevel { get; init; }
    public double? P25GlucoseLevel { get; init; }
    public double? P75GlucoseLevel { get; init; }
}
