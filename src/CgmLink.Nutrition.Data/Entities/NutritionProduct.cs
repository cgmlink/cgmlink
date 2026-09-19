using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Data.Entities;

/// <summary>
/// A nutrition product cached from an external source, retained for a limited period in the separate cache database.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("nutrition_products")]
[Index(nameof(ProductId))]
[Index(nameof(ExpiresAt))]
public class NutritionProduct
{
    /// <summary>
    /// The unique identifier for the cached product, assigned by us.
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// The id of the product in the nutrition source, such as a fatsecret food id.
    /// </summary>
    [MaxLength(450)]
    public required string ProductId { get; set; }

    /// <summary>
    /// The barcode of the product, when the cache entry was resolved from a barcode scan.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// The name of the product.
    /// </summary>
    [MaxLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// The calories for the product.
    /// </summary>
    public required decimal Calories { get; set; }

    /// <summary>
    /// The carbohydrates in grams for the product.
    /// </summary>
    public required decimal Carbs { get; set; }

    /// <summary>
    /// The protein in grams for the product.
    /// </summary>
    public required decimal Protein { get; set; }

    /// <summary>
    /// The fat in grams for the product.
    /// </summary>
    public required decimal Fat { get; set; }

    /// <summary>
    /// The date and time the cache entry expires and is considered stale.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; set; }
}