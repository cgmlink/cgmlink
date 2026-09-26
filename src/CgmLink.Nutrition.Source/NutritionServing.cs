namespace CgmLink.Nutrition.Source;

public sealed class NutritionServing
{
    public required string ExternalId { get; init; }

    public string? Description { get; init; }

    public decimal? ServingAmount { get; init; }

    public string? ServingUnit { get; init; }

    public decimal Calories { get; init; }

    public decimal Carbs { get; init; }

    public decimal Protein { get; init; }

    public decimal Fat { get; init; }
}
