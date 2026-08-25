using CgmLink.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CgmLink.Data.Migrators.MSSQL;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CgmLinkDbContext>
{
    public CgmLinkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CgmLinkDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CgmLinkDesignTime;Trusted_Connection=True;",
            sql => sql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.GetName().Name));

        return new CgmLinkDbContext(optionsBuilder.Options);
    }
}
