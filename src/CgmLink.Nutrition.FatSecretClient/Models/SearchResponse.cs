using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class SearchResponse
{
    [JsonPropertyName("foods")]
    public Foods? Foods { get; init; }
}
