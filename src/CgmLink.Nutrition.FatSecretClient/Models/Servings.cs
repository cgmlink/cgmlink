using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class Servings
{
    [JsonPropertyName("serving")]
    public Serving[] Items { get; init; } = [];
}
