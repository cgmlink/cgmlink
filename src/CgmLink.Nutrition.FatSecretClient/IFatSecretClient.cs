using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Nutrition.FatSecretClient.Models;

namespace CgmLink.Nutrition.FatSecretClient;

public interface IFatSecretClient
{
    Task<FatSecretSearchResults> SearchAsync(
        string searchExpression,
        int? pageNumber = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default);

    Task<FatSecretFood?> GetFoodAsync(string foodId, CancellationToken cancellationToken = default);
}
