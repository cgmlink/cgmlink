using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Data.Entities;

/// <summary>
/// An injection is a single insulin injection event, recording which insulin was used and how many units.
/// </summary>
[ExcludeFromCodeCoverage]
[Table("injections")]
public class Injection
{
    /// <summary>
    /// The unique identifier for the injection.
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The id of the user who administered the injection.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The user who administered the injection.
    /// </summary>
    public virtual User? User { get; set; }

    /// <summary>
    /// The id of the insulin used for the injection.
    /// </summary>
    public Guid InsulinId { get; set; }

    /// <summary>
    /// The insulin used for the injection.
    /// </summary>
    public virtual Insulin? Insulin { get; set; }

    /// <summary>
    /// The number of units injected.
    /// </summary>
    public required decimal Units { get; set; }

    /// <summary>
    /// The date and time the injection was administered.
    /// </summary>
    public required DateTimeOffset Created { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The date and time the injection was last updated.
    /// </summary>
    public DateTimeOffset? Updated { get; set; }
}
