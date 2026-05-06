using System.Diagnostics.CodeAnalysis;
using FluentValidation;
using CgmLink.Nutrition.Data;
using CgmLink.Nutrition.Data.Repository;
using CgmLink.Nutrition.Endpoints;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CgmLink.Nutrition;

[ExcludeFromCodeCoverage]
public static class Startup
{
    public static IServiceCollection AddNutrition(this IServiceCollection services, Action<NutritionOptions> configure)
    {
        services.AddApiVersioning();
        services.AddProblemDetails();
        services.AddValidatorsFromAssemblyContaining(typeof(Startup));

        services.AddOptions<NutritionOptions>()
            .Configure(configure)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddNutritionDbContext(configure);

        return services;
    }

    public static IEndpointRouteBuilder MapNutritionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapNutritionEndpointsInternal();
    }

    private static IServiceCollection AddNutritionDbContext(this IServiceCollection services, Action<NutritionOptions> configure)
    {
        var nutritionOptions = new NutritionOptions();
        configure?.Invoke(nutritionOptions);

        services.AddDbContext<CgmLinkNutritionDbContext>(options =>
        {
            options.UseSqlServer(nutritionOptions.ConnectionString,
                e => e.MigrationsAssembly("CgmLink.Nutrition.Data.Migrators.MSSQL"));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}