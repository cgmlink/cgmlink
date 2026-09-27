using CgmLink.Nutrition.Caching;
using CgmLink.Nutrition.Caching.Entities;

namespace CgmLink.Nutrition;

internal sealed class NullNutritionCache : INutritionCache
{
    public ValueTask<NutritionProduct?> GetAsync(
        string source,
        string productId,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<NutritionProduct?>(null);

    public ValueTask<NutritionProduct?> GetByBarcodeAsync(
        string source,
        string barcode,
        CancellationToken cancellationToken = default) => ValueTask.FromResult<NutritionProduct?>(null);

    public ValueTask SetAsync(NutritionProduct product, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
