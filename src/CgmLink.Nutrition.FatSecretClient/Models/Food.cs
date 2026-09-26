using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class Food
{
    [JsonPropertyName("food_id")]
    public string ProductId { get; init; } = "";

    [JsonPropertyName("food_name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("servings")]
    public Servings? Servings { get; init; }
}
