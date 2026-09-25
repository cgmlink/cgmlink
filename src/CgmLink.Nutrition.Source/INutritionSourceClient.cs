using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.Source;

public interface INutritionSourceClient
{
    string Source { get; }

    Task<IReadOnlyCollection<NutritionProduct>> SearchAsync(
        string searchExpression,
        int pageNumber = 0,
        int maxResults = 20,
        CancellationToken cancellationToken = default);

    Task<NutritionProduct?> GetAsync(string productId, CancellationToken cancellationToken = default);

    Task<NutritionProduct?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);
}
