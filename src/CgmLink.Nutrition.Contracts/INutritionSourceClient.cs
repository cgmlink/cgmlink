using CgmLink.Nutrition.Contracts.Models;

namespace CgmLink.Nutrition.Contracts;

/// <summary>
/// Retrieves normalized food data from an external nutrition source.
/// </summary>
public interface INutritionSourceClient
{
    string Source { get; }

    Task<NutritionSearchResults> SearchAsync(
        string searchExpression,
        int? pageNumber = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);

    Task<NutritionFood?> GetFoodAsync(
        string productId,
        CancellationToken cancellationToken = default);
}
