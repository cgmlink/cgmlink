using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// A permanent serving identity belonging to a nutrition ingredient.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("nutrition_servings")]
public class NutritionServing
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid NutritionIngredientId { get; set; }

    public virtual required NutritionIngredient NutritionIngredient { get; set; }

    [MaxLength(255)]
    public required string ServingId { get; set; }
}
