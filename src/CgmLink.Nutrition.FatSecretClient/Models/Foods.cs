using System.Text.Json;
using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class Foods
{
    [JsonPropertyName("food")]
    public JsonElement Food { get; init; }
}
