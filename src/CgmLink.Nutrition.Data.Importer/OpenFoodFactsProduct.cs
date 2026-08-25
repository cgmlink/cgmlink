using Newtonsoft.Json;

namespace CgmLink.Nutrition.Data.Importer;

public sealed class OpenFoodFactsProduct
{
    private const string ProductImagesBaseUrl = "https://images.openfoodfacts.org/images/products";

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

    [JsonProperty("brands")]
    public string? Brands { get; set; }

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

    [JsonProperty("lc")]
    public string? LanguageCode { get; set; }

    [JsonProperty("serving_quantity")]
    public double? ServingQuantity { get; set; }

    [JsonProperty("images")]
    public IDictionary<string, OpenFoodFactsImage>? Images { get; set; }

    public string? ImageUrl => BuildSelectedFrontImageUrl("full");

    public string? ImageThumbUrl => BuildSelectedFrontImageUrl("100");

    private string? BuildSelectedFrontImageUrl(string resolution)
    {
        var imageKey = GetFrontImageKey();
        if (string.IsNullOrWhiteSpace(imageKey) ||
            Images is null ||
            !Images.TryGetValue(imageKey, out var image) ||
            string.IsNullOrWhiteSpace(image.Rev))
        {
            return null;
        }

        var folder = ComputeProductImageFolder(Code);
        if (folder is null)
        {
            return null;
        }

        return $"{ProductImagesBaseUrl}/{folder}/{imageKey}.{image.Rev}.{resolution}.jpg";
    }

    private string? GetFrontImageKey()
    {
        if (Images is null || Images.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(LanguageCode))
        {
            var preferredKey = $"front_{LanguageCode}";
            if (Images.ContainsKey(preferredKey))
            {
                return preferredKey;
            }
        }

        return Images.Keys
            .Where(k => k.StartsWith("front_", StringComparison.OrdinalIgnoreCase))
            .OrderBy(k => k, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    public static string? ComputeProductImageFolder(string? code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Any(c => !char.IsDigit(c)))
        {
            return null;
        }

        var normalizedCode = code.Length < 13
            ? code.PadLeft(13, '0')
            : code;

        return $"{normalizedCode[..3]}/{normalizedCode[3..6]}/{normalizedCode[6..9]}/{normalizedCode[9..]}";
    }
}

public sealed class OpenFoodFactsImage
{
    [JsonProperty("rev")]
    public string? Rev { get; set; }
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

    [JsonProperty("energy_100g")]
    public double? Energy100g { get; set; }

    [JsonProperty("energy_serving")]
    public double? EnergyServing { get; set; }

    [JsonProperty("energy-kcal_100g")]
    public double? EnergyKcal100g { get; set; }

    [JsonProperty("energy-kcal_serving")]
    public double? EnergyKcalServing { get; set; }

    [JsonProperty("fat_100g")]
    public double? Fat100g { get; set; }

    [JsonProperty("fat_serving")]
    public double? FatServing { get; set; }

    [JsonProperty("carbohydrates_100g")]
    public double? Carbohydrates100g { get; set; }

    [JsonProperty("carbohydrates_serving")]
    public double? CarbohydratesServing { get; set; }

    [JsonProperty("proteins_100g")]
    public double? Proteins100g { get; set; }

    [JsonProperty("proteins_serving")]
    public double? ProteinsServing { get; set; }
}
