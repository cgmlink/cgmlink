using System.Net.Http;

namespace CgmLink.Nutrition.FatSecretClient;

internal sealed class FatSecretApiException(int errorCode, string message)
    : HttpRequestException($"FatSecret API error {errorCode}: {message}")
{
    public int ErrorCode { get; } = errorCode;
}
