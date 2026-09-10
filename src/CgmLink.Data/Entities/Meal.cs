using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// A meal is a combination of ingredients.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("meals")]
public class Meal : ISoftDeletable
{
    /// <summary>
    /// The unique identifier for the meal.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the user who created the meal.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user who created the meal.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// The name of the meal.
    /// </summary>
    [MaxLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// The URL of the meal image.
    /// </summary>
    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The URL of the meal thumbnail image.
    /// </summary>
    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// The date and time the meal was created.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the meal was last updated.
    /// </summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>
    /// The date and time the meal was soft-deleted.
    /// </summary>
    public DateTimeOffset? Deleted { get; set; }

    /// <summary>
    /// The ingredients that make up the meal.
    /// </summary>
    public virtual ICollection<MealIngredient> Ingredients { get; set; } = [];
}