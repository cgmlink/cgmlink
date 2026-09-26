using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class ErrorResponse
{
    [JsonPropertyName("error")]
    public Error? Error { get; init; }
}
