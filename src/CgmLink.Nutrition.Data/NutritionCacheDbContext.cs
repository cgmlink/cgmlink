using CgmLink.Nutrition.Data.Entities;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Data;

[ExcludeFromCodeCoverage]
public class NutritionCacheDbContext : DbContext
{
    public DbSet<NutritionProduct> NutritionProducts { get; set; }

    public NutritionCacheDbContext(DbContextOptions<NutritionCacheDbContext> options) : base(options)
    {
    }
}