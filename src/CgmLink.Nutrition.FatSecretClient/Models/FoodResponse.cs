using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class FoodResponse
{
    [JsonPropertyName("food")]
    public Food? Food { get; init; }
}
