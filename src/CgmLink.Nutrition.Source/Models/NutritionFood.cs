namespace CgmLink.Nutrition.Source.Models;

/// <summary>
/// A food returned by a nutrition data source, normalized for use by CGM Link.
/// </summary>
public sealed record NutritionFood
{
    public required string ProductId { get; init; }

    public required string Name { get; init; }

    public IReadOnlyList<NutritionServing> Servings { get; init; } = [];
}
