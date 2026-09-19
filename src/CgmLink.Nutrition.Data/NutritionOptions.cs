using System;
using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition;

/// <summary>
/// Represents the options for the nutrition cache.
/// </summary>
public sealed class NutritionOptions
{
    /// <summary>
    /// The connection string of the separate nutrition cache database.
    /// </summary>
    [Required]
    public string CacheConnectionString { get; init; } = "";

    /// <summary>
    /// How long a nutrition source response may be retained in the cache before it is considered expired.
    /// </summary>
    [Required]
    public TimeSpan CacheExpiry { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// How often expired nutrition cache entries are deleted from the cache database.
    /// </summary>
    [Required]
    public TimeSpan CacheCleanupInterval { get; init; } = TimeSpan.FromHours(1);
}