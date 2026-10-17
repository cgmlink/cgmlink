using System.ComponentModel.DataAnnotations;

namespace CgmLink.Api.Endpoints.Insights.MealGlucose;

public sealed class MealGlucoseSettings
{
    [Range(1, int.MaxValue)]
    public int WindowMinutes { get; set; }

    [Range(1, int.MaxValue)]
    public int DefaultIntervalMinutes { get; set; }

    [Range(1, int.MaxValue)]
    public int MinimumIntervalMinutes { get; set; }

    [Range(0, int.MaxValue)]
    public int BaselineMinutes { get; set; }

    [Range(0, int.MaxValue)]
    public int ToleranceMinutes { get; set; }
}
