using System.Text.Json.Serialization;

namespace CgmLink.Nutrition.FatSecretClient.Models;

internal sealed class Serving
{
    [JsonPropertyName("serving_id")]
    public string ExternalId { get; init; } = "";

    [JsonPropertyName("serving_description")]
    public string? Description { get; init; }

    [JsonPropertyName("metric_serving_amount")]
    public decimal? ServingAmount { get; init; }

    [JsonPropertyName("metric_serving_unit")]
    public string? ServingUnit { get; init; }

    [JsonPropertyName("calories")]
    public decimal Calories { get; init; }

    [JsonPropertyName("carbohydrate")]
    public decimal Carbs { get; init; }

    [JsonPropertyName("protein")]
    public decimal Protein { get; init; }

    [JsonPropertyName("fat")]
    public decimal Fat { get; init; }
}
