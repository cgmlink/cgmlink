using CgmLink.Nutrition.Source;

namespace CgmLink.Nutrition;

public interface INutritionCatalog
{
    Task<IReadOnlyCollection<NutritionProduct>> SearchAsync(
        string searchExpression,
        int pageNumber = 0,
        int maxResults = 20,
        CancellationToken cancellationToken = default);

    Task<NutritionProduct?> GetAsync(string productId, CancellationToken cancellationToken = default);

    Task<NutritionProduct?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<NutritionProduct?> RefreshAsync(string productId, CancellationToken cancellationToken = default);
}
