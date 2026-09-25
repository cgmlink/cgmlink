namespace CgmLink.Nutrition.Source.Models;

public sealed record NutritionFoodSearchResult
{
    public required string ProductId { get; init; }

    public required string Name { get; init; }
}
