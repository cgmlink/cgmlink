using System;
using System.ComponentModel.DataAnnotations;

namespace CgmLink.Nutrition.Data;

public sealed class NutritionOptions
{
    [Required]
    public NutritionCacheProvider CacheProvider { get; init; } = NutritionCacheProvider.Mssql;

    [Required]
    public string CacheConnectionString { get; init; } = "";

    [Required]
    public TimeSpan CacheExpiry { get; init; } = TimeSpan.FromHours(24);

    [Required]
    public TimeSpan CacheCleanupInterval { get; init; } = TimeSpan.FromHours(1);
}