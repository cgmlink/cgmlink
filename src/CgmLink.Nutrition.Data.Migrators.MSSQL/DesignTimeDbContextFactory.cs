using CgmLink.Nutrition.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CgmLink.Nutrition.Migrators.MSSQL;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<NutritionCacheDbContext>
{
    public NutritionCacheDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NutritionCacheDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=CgmLinkNutritionDesignTime;Trusted_Connection=True;",
            sql => sql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.GetName().Name));

        return new NutritionCacheDbContext(optionsBuilder.Options);
    }
}