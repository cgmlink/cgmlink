using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
            || string.IsNullOrWhiteSpace(fatSecretOptions.ConsumerKey)
            || string.IsNullOrWhiteSpace(fatSecretOptions.ConsumerSecret))
        {
            return services;
        }

        services.AddOptions<FatSecretOptions>()
            .Configure(options => fatSecretSection.Bind(options))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}