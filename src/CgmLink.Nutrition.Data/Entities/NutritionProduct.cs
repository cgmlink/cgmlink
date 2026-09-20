using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Data.Entities;

[ExcludeFromCodeCoverage]
[PrimaryKey(nameof(Source), nameof(ProductId))]
[Table("nutrition_products")]
public class NutritionProduct
{
    [MaxLength(32)]
    public required string Source { get; set; }

    [MaxLength(450)]
    public required string ProductId { get; set; }

    public string? Barcode { get; set; }

    [MaxLength(255)]
    public required string Name { get; set; }

    public required decimal Calories { get; set; }

    public required decimal Carbs { get; set; }

    public required decimal Protein { get; set; }

    public required decimal Fat { get; set; }

    public required DateTimeOffset ExpiresAt { get; set; }
}