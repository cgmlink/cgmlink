using CgmLink.Nutrition.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CgmLink.Nutrition.Data.Migrators.MSSQL;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CgmLinkNutritionDbContext>
{
    public CgmLinkNutritionDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CgmLinkNutritionDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CgmLinkNutritionDesignTime;Trusted_Connection=True;",
            sql => sql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.GetName().Name));

        return new CgmLinkNutritionDbContext(optionsBuilder.Options);
    }
}
