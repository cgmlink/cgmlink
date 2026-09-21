using System;
using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition.Caching;

public sealed class NutritionOptions
{
    [Required]
    public NutritionCacheProvider CacheProvider { get; init; } = NutritionCacheProvider.Ef;

    [Required]
    public string CacheConnectionString { get; init; } = "";

    [Required]
    public TimeSpan CacheExpiry { get; init; } = TimeSpan.FromHours(24);

    [Required]
    public TimeSpan CacheCleanupInterval { get; init; } = TimeSpan.FromHours(1);
}