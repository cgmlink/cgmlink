using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data.Entities;

/// <summary>
/// Links a treatment to a meal included in that treatment.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("treatment_meals")]
public class TreatmentMeal
{
    /// <summary>
    /// The unique identifier for the treatment meal link.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the treatment the meal belongs to.
    /// </summary>
    public Guid TreatmentId { get; set; }

    /// <summary>
    /// The treatment the meal belongs to.
    /// </summary>
    public virtual Treatment? Treatment { get; set; }

    /// <summary>
    /// The id of the meal included in the treatment.
    /// </summary>
    public Guid MealId { get; set; }

    /// <summary>
    /// The meal included in the treatment.
    /// </summary>
    [DeleteBehavior(DeleteBehavior.NoAction)]
    public virtual Meal? Meal { get; set; }

    /// <summary>
    /// The quantity of the meal included in the treatment.
    /// </summary>
    public required decimal Quantity { get; set; }

    /// <summary>
    /// The date and time the meal was added to the treatment.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
