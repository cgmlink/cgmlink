using CgmLink.Nutrition.Source;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition.FatSecretClient;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFatSecretClient(this IServiceCollection services, IConfiguration configuration)
    {
        var fatSecretSection = configuration.GetSection("FatSecret");
        var fatSecretOptions = fatSecretSection.Get<FatSecretOptions>();
        if (fatSecretOptions is null
            || string.IsNullOrWhiteSpace(fatSecretOptions.ClientId)
            || string.IsNullOrWhiteSpace(fatSecretOptions.ClientSecret))
        {
            return services;
        }

        services.AddOptions<FatSecretOptions>()
            .Configure(options => fatSecretSection.Bind(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient("FatSecretAuth");
        services.AddSingleton<IFatSecretAuthenticator, FatSecretAuthenticator>();

        services.AddHttpClient<INutritionSourceClient, FatSecretClient>(client =>
        {
            client.BaseAddress = new Uri(fatSecretOptions.ApiBaseUrl);
        });

        return services;
    }
}
