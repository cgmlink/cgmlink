using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition.Caching.Entities;

[ExcludeFromCodeCoverage]
[Table("nutrition_servings")]
public class NutritionServing
{
    [MaxLength(32)]
    public required string Source { get; set; }

    [MaxLength(128)]
    public required string ProductId { get; set; }

    [MaxLength(128)]
    public required string ServingId { get; set; }

    [MaxLength(255)]
    public string? Description { get; set; }

    public decimal? ServingAmount { get; set; }

    [MaxLength(64)]
    public string? ServingUnit { get; set; }

    public decimal Calories { get; set; }

    public decimal Carbs { get; set; }

    public decimal Protein { get; set; }

    public decimal Fat { get; set; }

    public NutritionProduct Product { get; set; } = null!;
}
