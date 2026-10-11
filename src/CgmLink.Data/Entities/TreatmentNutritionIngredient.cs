using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data.Entities;

/// <summary>
/// Links a treatment to an external nutrition ingredient with the serving and quantity used.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("treatment_nutrition_ingredients")]
public class TreatmentNutritionIngredient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TreatmentId { get; set; }

    public virtual Treatment? Treatment { get; set; }

    public Guid NutritionIngredientId { get; set; }

    public virtual NutritionIngredient? NutritionIngredient { get; set; }

    public Guid ServingId { get; set; }

    [DeleteBehavior(DeleteBehavior.NoAction)]
    public virtual NutritionServing? Serving { get; set; }

    public required decimal Quantity { get; set; }

    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}
