using CgmLink.Nutrition.Caching;
using CgmLink.Nutrition.Caching.Ef;
using CgmLink.Nutrition.Endpoints;
using CgmLink.Nutrition.FatSecretClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

        switch (nutritionOptions.CacheProvider)
        {
            case NutritionCacheProvider.Ef:
                services.AddNutritionCacheEf(configuration);
                break;
            default:
                throw new NotSupportedException($"Nutrition cache provider '{nutritionOptions.CacheProvider}' is not supported yet.");
        }

        services.AddFatSecretClient(configuration);

        return services;
    }

    public static IEndpointRouteBuilder MapNutritionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapNutritionEndpointsInternal();
    }
}