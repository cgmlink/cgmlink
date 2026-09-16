using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data.Entities;

/// <summary>
/// Links a treatment to an ingredient with the quantity used.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("treatment_ingredients")]
public class TreatmentIngredient
{
    /// <summary>
    /// The unique identifier for the treatment ingredient link.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the treatment the ingredient belongs to.
    /// </summary>
    public Guid TreatmentId { get; set; }

    /// <summary>
    /// The treatment the ingredient belongs to.
    /// </summary>
    public virtual Treatment? Treatment { get; set; }

    /// <summary>
    /// The id of the ingredient.
    /// </summary>
    public Guid IngredientId { get; set; }

    /// <summary>
    /// The ingredient.
    /// </summary>
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public virtual Ingredient? Ingredient { get; set; }

    /// <summary>
    /// The id of the serving the quantity is measured in.
    /// </summary>
    public Guid ServingId { get; set; }

    /// <summary>
    /// The serving the quantity is measured in.
    /// </summary>
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public virtual IngredientServing? Serving { get; set; }

    /// <summary>
    /// The quantity of the serving.
    /// </summary>
    public required decimal Quantity { get; set; }

    /// <summary>
    /// The date and time the ingredient was added to the treatment.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
