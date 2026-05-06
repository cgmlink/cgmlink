using System.Diagnostics.CodeAnalysis;
using CgmLink.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Data;

[ExcludeFromCodeCoverage]
public class CgmLinkDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<Reading> Readings { get; set; }
    public DbSet<Ingredient> Ingredients { get; set; }
    public DbSet<Meal> Meals { get; set; }
    public DbSet<MealIngredient> MealIngredients { get; set; }
    public DbSet<Insulin> Insulins { get; set; }
    public DbSet<Injection> Injections { get; set; }
    public DbSet<Treatment> Treatments { get; set; }
    public DbSet<TreatmentIngredient> TreatmentIngredients { get; set; }
    public DbSet<TreatmentMeal> TreatmentMeals { get; set; }
    public DbSet<Sensor> Sensors { get; set; }
    public DbSet<AlarmRule> AlarmRules { get; set; }
    public DbSet<Pen> Pens { get; set; }

    public CgmLinkDbContext(DbContextOptions<CgmLinkDbContext> options) : base(options)
    {
    }
}