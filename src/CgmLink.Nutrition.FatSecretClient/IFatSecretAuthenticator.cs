using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.FatSecretClient;

internal interface IFatSecretAuthenticator
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
