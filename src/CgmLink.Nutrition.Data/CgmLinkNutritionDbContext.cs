using System.Diagnostics.CodeAnalysis;
using CgmLink.Nutrition.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Data;

[ExcludeFromCodeCoverage]
public class CgmLinkNutritionDbContext : DbContext
{
    public DbSet<Product> Products { get; set; }

    public CgmLinkNutritionDbContext(DbContextOptions<CgmLinkNutritionDbContext> options) : base(options)
    {
    }
}