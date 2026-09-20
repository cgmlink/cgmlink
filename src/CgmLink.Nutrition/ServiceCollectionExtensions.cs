using CgmLink.Nutrition.Data;
using CgmLink.Nutrition.Data.Sql;
using CgmLink.Nutrition.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNutrition(this IServiceCollection services, IConfiguration configuration)
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

        var fatSecretSection = configuration.GetSection("FatSecret");
        var fatSecretOptions = fatSecretSection.Get<FatSecretOptions>();
        if (fatSecretOptions is not null
            && !string.IsNullOrWhiteSpace(fatSecretOptions.ConsumerKey)
            && !string.IsNullOrWhiteSpace(fatSecretOptions.ConsumerSecret))
        {
            services.AddOptions<FatSecretOptions>()
                .Configure(fatSecretSection.Bind)
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }

        switch (nutritionOptions.CacheProvider)
        {
            case NutritionCacheProvider.Mssql:
                services.AddDbContext<NutritionCacheDbContext>((provider, options) =>
                {
                    var cacheOptions = provider.GetRequiredService<IOptions<NutritionOptions>>().Value;
                    options.UseSqlServer(cacheOptions.CacheConnectionString,
                        e => e.MigrationsAssembly("CgmLink.Nutrition.Data.Migrators.MSSQL"));
                });

                services.AddScoped<INutritionCache, SqlServerNutritionCache>();
                services.AddScoped<NutritionDbInitializer>();
                services.AddHostedService<SqlCacheCleanupService>();
                break;
            default:
                throw new NotSupportedException($"Nutrition cache provider '{nutritionOptions.CacheProvider}' is not supported yet.");
        }

        return services;
    }

    public static IEndpointRouteBuilder MapNutritionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapNutritionEndpointsInternal();
    }
}