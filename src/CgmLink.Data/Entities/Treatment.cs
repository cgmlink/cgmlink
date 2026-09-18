using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// A treatment is a snapshot of a user's diabetes management event, capturing a reading,
/// an injection, and the food consumed as meals and ingredients with aggregated nutrition totals.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("treatments")]
public class Treatment : ISoftDeletable
{
    /// <summary>
    /// The unique identifier for the treatment.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the user who created the treatment.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user who created the treatment.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// The id of the glucose reading associated with this treatment.
    /// </summary>
    public Guid? ReadingId { get; set; }

    /// <summary>
    /// The glucose reading associated with this treatment.
    /// </summary>
    public virtual Reading? Reading { get; set; }

    /// <summary>
    /// The id of the injection associated with this treatment.
    /// </summary>
    public Guid? InjectionId { get; set; }

    /// <summary>
    /// The injection associated with this treatment.
    /// </summary>
    public virtual Injection? Injection { get; set; }

    /// <summary>
    /// The snapshot total calories for this treatment.
    /// </summary>
    public required decimal Calories { get; set; }

    /// <summary>
    /// The snapshot total carbohydrates in grams for this treatment.
    /// </summary>
    public required decimal Carbs { get; set; }

    /// <summary>
    /// The snapshot total protein in grams for this treatment.
    /// </summary>
    public required decimal Protein { get; set; }

    /// <summary>
    /// The snapshot total fat in grams for this treatment.
    /// </summary>
    public required decimal Fat { get; set; }

    /// <summary>
    /// The date and time the treatment was created.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the treatment was last updated.
    /// </summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>
    /// The date and time the treatment was soft-deleted.
    /// </summary>
    public DateTimeOffset? Deleted { get; set; }

    /// <summary>
    /// The meals included in this treatment.
    /// </summary>
    public virtual ICollection<TreatmentMeal> Meals { get; set; } = [];

    /// <summary>
    /// The ingredients included in this treatment.
    /// </summary>
    public virtual ICollection<TreatmentIngredient> Ingredients { get; set; } = [];
}
