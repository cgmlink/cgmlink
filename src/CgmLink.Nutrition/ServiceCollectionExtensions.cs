using CgmLink.Nutrition.Data;
using CgmLink.Nutrition.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds nutrition services to the service collection.
    /// </summary>
    /// <param name="services">Service collection to add nutrition services to.</param>
    /// <param name="configureNutrition">Configures the <see cref="NutritionOptions"/>.</param>
    /// <param name="configureFatSecret">Configures the <see cref="FatSecretOptions"/> when fatsecret is used as the nutrition source.</param>
    /// <returns>Service collection containing nutrition services.</returns>
    public static IServiceCollection AddNutrition(
        this IServiceCollection services,
        Action<NutritionOptions> configureNutrition,
        Action<FatSecretOptions>? configureFatSecret = null)
    {
        services.AddOptions<NutritionOptions>()
            .Configure(configureNutrition)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        if (configureFatSecret is not null)
        {
            services.AddOptions<FatSecretOptions>()
                .Configure(configureFatSecret)
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

    /// <summary>
    /// Maps nutrition endpoints to the application.
    /// </summary>
    /// <param name="endpoints">Endpoint route builder to map nutrition endpoints.</param>
    /// <returns>Endpoint route builder with nutrition endpoints mapped.</returns>
    public static IEndpointRouteBuilder MapNutritionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapNutritionEndpointsInternal();
    }
}