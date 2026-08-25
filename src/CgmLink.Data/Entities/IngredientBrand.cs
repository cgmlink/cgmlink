using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// A brand associated with an ingredient, sourced from the nutrition data provider.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("ingredients_brands")]
public class IngredientBrand
{
    /// <summary>
    /// The unique identifier for the ingredient brand.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The unique identifier for the ingredient.
    /// </summary>
    public required Guid IngredientId { get; set; }

    /// <summary>
    /// The ingredient this brand is associated with.
    /// </summary>
    public virtual Ingredient? Ingredient { get; set; }

    /// <summary>
    /// The brand name.
    /// </summary>
    public required string Name { get; set; }
}
