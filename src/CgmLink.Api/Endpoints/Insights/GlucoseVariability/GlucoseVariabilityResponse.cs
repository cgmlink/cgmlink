using System;

namespace CgmLink.Api.Endpoints.Insights.GlucoseVariability;

public record GlucoseVariabilityResponse
{
    public required DateTimeOffset From { get; init; }
    public required DateTimeOffset To { get; init; }
    public required double StandardDeviation { get; init; }
    public required double CoefficientOfVariation { get; init; }
}
