using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.Caching;

public interface INutritionDbInitializer
{
    Task InitialiseCacheAsync(CancellationToken cancellationToken);
}