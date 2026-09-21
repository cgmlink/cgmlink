using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition.Caching.Ef;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNutritionCacheEf(this IServiceCollection services, IConfiguration configuration)
    {
        var nutritionSection = configuration.GetSection("Nutrition");
        var nutritionOptions = nutritionSection.Get<NutritionOptions>();
        if (string.IsNullOrWhiteSpace(nutritionOptions?.CacheConnectionString))
        {
            return services;
        }

        services.AddOptions<NutritionOptions>()
            .Configure(nutritionSection.Bind)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<NutritionCacheDbContext>((provider, options) =>
        {
            var cacheOptions = provider.GetRequiredService<IOptions<NutritionOptions>>().Value;
            options.UseSqlServer(cacheOptions.CacheConnectionString,
                e => e.MigrationsAssembly("CgmLink.Nutrition.Caching.Ef.Migrators.MSSQL"));
        });

        services.AddScoped<INutritionCache, SqlServerNutritionCache>();
        services.AddScoped<INutritionDbInitializer, NutritionDbInitializer>();
        services.AddHostedService<SqlCacheCleanupService>();

        return services;
    }
}