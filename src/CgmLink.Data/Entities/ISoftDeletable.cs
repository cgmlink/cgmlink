using System;

namespace CgmLink.Data.Entities;

/// <summary>
/// Represents an entity that can be soft-deleted.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// The date and time the entity was soft-deleted.
    /// </summary>
    DateTimeOffset? Deleted { get; set; }
}