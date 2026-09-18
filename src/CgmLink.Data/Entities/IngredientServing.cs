using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// A serving size of an ingredient, with nutrition values for that serving.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("ingredient_servings")]
public class IngredientServing : ISoftDeletable
{
    /// <summary>
    /// The unique identifier for the serving.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the ingredient the serving belongs to.
    /// </summary>
    public Guid IngredientId { get; set; }

    /// <summary>
    /// The ingredient the serving belongs to.
    /// </summary>
    public virtual Ingredient? Ingredient { get; set; }

    /// <summary>
    /// The id of the serving in the external api, when created from a barcode scan.
    /// </summary>
    public string? ExternalId { get; set; }

    /// <summary>
    /// A human readable description of the serving, such as "1 cup" or "100g".
    /// </summary>
    [MaxLength(64)]
    public string? Description { get; set; }

    /// <summary>
    /// The metric amount of the serving, as provided by the external api. Informational only.
    /// </summary>
    public decimal? ServingAmount { get; set; }

    /// <summary>
    /// The metric unit of the serving, as provided by the external api. Informational only.
    /// </summary>
    [MaxLength(16)]
    public string? ServingUnit { get; set; }

    /// <summary>
    /// The calories for this serving.
    /// </summary>
    public required decimal Calories { get; set; }

    /// <summary>
    /// The carbohydrates in grams for this serving.
    /// </summary>
    public required decimal Carbs { get; set; }

    /// <summary>
    /// The protein in grams for this serving.
    /// </summary>
    public required decimal Protein { get; set; }

    /// <summary>
    /// The fat in grams for this serving.
    /// </summary>
    public required decimal Fat { get; set; }

    /// <summary>
    /// The date and time the serving was created.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the serving was soft-deleted. When set, meals that referenced this
    /// serving keep their snapshot, but the serving is no longer offered for the ingredient.
    /// </summary>
    public DateTimeOffset? Deleted { get; set; }
}