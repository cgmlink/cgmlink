using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data.Entities;

/// <summary>
/// Links a meal to an ingredient with the quantity used.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("meal_ingredients")]
public class MealIngredient
{
    /// <summary>
    /// The unique identifier for the meal ingredient.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the meal the ingredient belongs to.
    /// </summary>
    public Guid MealId { get; set; }

    /// <summary>
    /// The meal the ingredient belongs to.
    /// </summary>
    public virtual Meal? Meal { get; set; }

    /// <summary>
    /// The id of the ingredient.
    /// </summary>
    public Guid IngredientId { get; set; }

    /// <summary>
    /// The ingredient.
    /// </summary>
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
    /// The date and time the ingredient was added to the meal.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}