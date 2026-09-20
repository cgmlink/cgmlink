using CgmLink.Nutrition.Data.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.Data;

public interface INutritionCache
{
    ValueTask<NutritionProduct?> GetAsync(string source, string productId, CancellationToken cancellationToken = default);

    ValueTask SetAsync(NutritionProduct product, CancellationToken cancellationToken = default);
}