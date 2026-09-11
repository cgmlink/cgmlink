using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data.Entities;

/// <summary>
/// An ingredient is a food item that can form part of a meal. Ingredients are shared between users
/// and linked by barcode, so each scanned product resolves to a single row.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("ingredients")]
[Index(nameof(Barcode), IsUnique = true)]
public class Ingredient : ISoftDeletable
{
    /// <summary>
    /// The unique identifier for the ingredient.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The name of the ingredient.
    /// </summary>
    [MaxLength(255)]
    public required string Name { get; set; }

    /// <summary>
    /// The barcode of the ingredient, unique across all users, when scanned.
    /// </summary>
    public string? Barcode { get; set; }

    /// <summary>
    /// The id of the linked external product, when created from a barcode scan.
    /// </summary>
    public string? ProductId { get; set; }

    /// <summary>
    /// The URL of the ingredient image.
    /// </summary>
    [MaxLength(2048)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The URL of the ingredient thumbnail image.
    /// </summary>
    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// The date and time the ingredient was created.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the ingredient was last updated.
    /// </summary>
    public DateTimeOffset? Updated { get; set; }

    /// <summary>
    /// The date and time the ingredient was soft-deleted.
    /// </summary>
    public DateTimeOffset? Deleted { get; set; }

    /// <summary>
    /// The serving sizes available for the ingredient.
    /// </summary>
    public virtual ICollection<IngredientServing> Servings { get; set; } = [];

    /// <summary>
    /// The users linked to this ingredient.
    /// </summary>
    public virtual ICollection<UserIngredient> Users { get; set; } = [];
}