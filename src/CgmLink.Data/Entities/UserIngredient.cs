using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data.Entities;

/// <summary>
/// Links a user to a shared ingredient.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("user_ingredients")]
[Index(nameof(UserId), nameof(IngredientId), IsUnique = true)]
public class UserIngredient
{
    /// <summary>
    /// The unique identifier for the user ingredient link.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the user linked to the ingredient.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user linked to the ingredient.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// The id of the ingredient linked to the user.
    /// </summary>
    public Guid IngredientId { get; set; }

    /// <summary>
    /// The ingredient linked to the user.
    /// </summary>
    public virtual Ingredient? Ingredient { get; set; }

    /// <summary>
    /// The date and time the link was created.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;
}