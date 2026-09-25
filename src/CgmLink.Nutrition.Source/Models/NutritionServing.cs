namespace CgmLink.Nutrition.Source.Models;

/// <summary>
/// A normalized serving whose nutrition values use the same units as an ingredient serving.
/// </summary>
public sealed record NutritionServing
{
    public string? ExternalId { get; init; }

    public string? Description { get; init; }

    public decimal? ServingAmount { get; init; }

    public string? ServingUnit { get; init; }

    public required decimal Calories { get; init; }

    public required decimal Carbs { get; init; }

    public required decimal Protein { get; init; }

    public required decimal Fat { get; init; }
}
