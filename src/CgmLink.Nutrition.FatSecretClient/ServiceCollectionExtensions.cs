using CgmLink.Nutrition.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient(FatSecretAccessTokenProvider.HttpClientName);
        services.AddSingleton<FatSecretAccessTokenProvider>();
        services.AddSingleton<IFatSecretAccessTokenProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<FatSecretAccessTokenProvider>());
        services.AddHttpClient<IFatSecretClient, FatSecretClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<FatSecretOptions>>().Value;
            client.BaseAddress = new Uri(options.ApiBaseUrl, UriKind.Absolute);
        });
        services.TryAddTransient<INutritionSourceClient>(serviceProvider =>
            serviceProvider.GetRequiredService<IFatSecretClient>());

        return services;
    }
}
