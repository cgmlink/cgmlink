using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// A permanent identity for a product from an external nutrition source.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("nutrition_ingredients")]
public class NutritionIngredient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public required string Source { get; set; }

    [MaxLength(255)]
    public required string ProductId { get; set; }

    public virtual ICollection<NutritionServing> Servings { get; set; } = [];
}
