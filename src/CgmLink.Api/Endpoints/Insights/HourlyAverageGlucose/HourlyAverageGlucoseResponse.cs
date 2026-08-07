namespace CgmLink.Api.Endpoints.Insights.HourlyAverageGlucose;

public record HourlyAverageGlucoseResponse
{
    public int Hour { get; init; }
    public double? AverageGlucoseLevel { get; init; }
    public int ReadingCount { get; init; }
}
