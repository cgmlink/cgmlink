using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CgmLink.Nutrition.Data.Importer;

public sealed class OpenFoodFactsProduct
{
    [JsonProperty("_id")]
    public string? Id { get; set; }

    [JsonProperty("product_type")]
    public string? ProductType { get; set; }

    [JsonProperty("quantity")]
    public string? Quantity { get; set; }

    [JsonProperty("product_quantity_unit")]
    public string? ProductQuantityUnit { get; set; }

    [JsonProperty("product_name")]
    public string? ProductName { get; set; }

    [JsonProperty("product_quantity")]
    public double? ProductQuantity { get; set; }

    [JsonProperty("nutrition_data_per")]
    public string? NutritionDataPer { get; set; }

    [JsonProperty("nutriments")]
    public OpenFoodFactsNutriments? Nutriments { get; set; }

    [JsonProperty("nutrition_data_prepared_per")]
    public string? NutritionDataPreparedPer { get; set; }

    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("serving_quantity")]
    public double? ServingQuantity { get; set; }

    [JsonProperty("image_front_url")]
    public string? ImageUrlValue { get; set; }

    [JsonProperty("image_front_thumb_url")]
    public string? ImageThumbUrlValue { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JToken>? AdditionalData { get; set; }

    public string? ImageUrl => FirstNonEmpty(ImageUrlValue, AdditionalData, "ImageUrl", "imageUrl");

    public string? ImageThumbUrl => FirstNonEmpty(ImageThumbUrlValue, AdditionalData, "ImageThumbUrl", "imageThumbUrl");

    private static string? FirstNonEmpty(string? primary, IDictionary<string, JToken>? additionalData, params string[] keys)
    {
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        if (additionalData is null)
        {
            return null;
        }

        foreach (var key in keys)
        {
            if (additionalData.TryGetValue(key, out var value))
            {
                var candidate = value.ToObject<string>();
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}

public sealed class OpenFoodFactsNutriments
{
    [JsonProperty("energy_unit")]
    public string? EnergyUnit { get; set; }

    [JsonProperty("fat_unit")]
    public string? FatUnit { get; set; }

    [JsonProperty("carbohydrates_unit")]
    public string? CarbohydratesUnit { get; set; }

    [JsonProperty("energy-kcal_unit")]
    public string? EnergyKcalUnit { get; set; }

    [JsonProperty("energy-kcal_value")]
    public double? EnergyKcalValue { get; set; }

    [JsonProperty("energy_value")]
    public double? EnergyValue { get; set; }

    [JsonProperty("carbohydrates_value")]
    public double? CarbohydratesValue { get; set; }

    [JsonProperty("proteins")]
    public float? Proteins { get; set; }

    [JsonProperty("energy-kcal_value_computed")]
    public double? EnergyKcalValueComputed { get; set; }

    [JsonProperty("proteins_value")]
    public float? ProteinsValue { get; set; }

    [JsonProperty("energy-kcal")]
    public double? EnergyKcal { get; set; }

    [JsonProperty("proteins_unit")]
    public string? ProteinsUnit { get; set; }

    [JsonProperty("carbohydrates")]
    public double? Carbohydrates { get; set; }

    [JsonProperty("energy")]
    public float? Energy { get; set; }

    [JsonProperty("fat")]
    public float? Fat { get; set; }

    [JsonProperty("fat_value")]
    public float? FatValue { get; set; }
}
