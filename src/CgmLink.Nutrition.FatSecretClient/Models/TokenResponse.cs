using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}
