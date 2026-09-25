namespace CgmLink.Nutrition.Contracts.Models;

public sealed record NutritionFoodSearchResult
{
    public required string ProductId { get; init; }

    public required string Name { get; init; }
}
