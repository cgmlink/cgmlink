using CgmLink.Nutrition.Caching.Entities;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Caching.Ef;

[ExcludeFromCodeCoverage]
public class NutritionCacheDbContext : DbContext
{
    public DbSet<NutritionProduct> NutritionProducts { get; set; }

    public NutritionCacheDbContext(DbContextOptions<NutritionCacheDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NutritionProduct>()
            .HasKey(p => new { p.Source, p.ProductId });
    }
}