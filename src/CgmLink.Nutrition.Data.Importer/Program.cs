using CgmLink.Nutrition.Data.Importer;
using CgmLink.Nutrition.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(configuration => configuration.AddUserSecrets<Program>())
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<CgmLinkNutritionDbContext>((_, options) =>
        {
            options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"),
                e => e.MigrationsAssembly("CgmLink.Nutrition.Data.Migrators.MSSQL"));
        });
    })
    .Build();

await host.Services.GetRequiredService<CgmLinkNutritionDbContext>().Database.MigrateAsync();

var importerOptions = ImporterOptions.Parse(args, host.Services.GetRequiredService<IConfiguration>());

using var scope = host.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<CgmLinkNutritionDbContext>();

await ImportRunner.RunAsync(dbContext, importerOptions);
