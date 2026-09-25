namespace CgmLink.Nutrition.Source.Models;

public sealed record NutritionSearchResults(
    IReadOnlyList<NutritionFoodSearchResult> Items,
    int? TotalResults = null,
    int? PageNumber = null,
    int? MaxResults = null);
