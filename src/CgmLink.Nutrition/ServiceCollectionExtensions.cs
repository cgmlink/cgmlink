using CgmLink.Nutrition.Data;
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
        if (string.IsNullOrWhiteSpace(nutritionSection.Get<NutritionOptions>()?.CacheConnectionString))
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

        services.AddDbContext<NutritionCacheDbContext>((provider, options) =>
        {
            var nutritionOptions = provider.GetRequiredService<IOptions<NutritionOptions>>().Value;
            options.UseSqlServer(nutritionOptions.CacheConnectionString,
                e => e.MigrationsAssembly("CgmLink.Nutrition.Data.Migrators.MSSQL"));
        });

        services.AddScoped<NutritionDbInitializer>();
        services.AddHostedService<NutritionCacheCleanupService>();

        return services;
    }

    public static IEndpointRouteBuilder MapNutritionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapNutritionEndpointsInternal();
    }
}