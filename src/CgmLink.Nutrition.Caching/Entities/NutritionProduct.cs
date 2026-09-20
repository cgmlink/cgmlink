using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition.Caching.Entities;

[ExcludeFromCodeCoverage]
[Table("nutrition_products")]
public class NutritionProduct
{
    /// <summary>
    /// The nutrition data source the product was fetched from, e.g. "fatsecret".
    /// </summary>
    [MaxLength(32)]
    public required string Source { get; set; }

    /// <summary>
    /// The id of the product in the data source.
    /// </summary>
    [MaxLength(450)]
    public required string ProductId { get; set; }

    /// <summary>
    /// The barcode of the product, when known.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// The name of the product.
    /// </summary>
    [MaxLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// The calories of the product, in kilocalories.
    /// </summary>
    public required decimal Calories { get; set; }

    /// <summary>
    /// The carbohydrate content of the product, in grams.
    /// </summary>
    public required decimal Carbs { get; set; }

    /// <summary>
    /// The protein content of the product, in grams.
    /// </summary>
    public required decimal Protein { get; set; }

    /// <summary>
    /// The fat content of the product, in grams.
    /// </summary>
    public required decimal Fat { get; set; }

    /// <summary>
    /// The timestamp after which the cached product is considered stale and must be
    /// refetched from the data source.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }
}