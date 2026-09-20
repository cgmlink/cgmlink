using System.Collections.Generic;
using CgmLink.Nutrition.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class ServiceCollectionExtensionsTests
{
    private const string ValidCacheConnectionString = "Server=.;Database=NutritionCacheTest;Trusted_Connection=True;";

    [Test]
    public void AddNutrition_RegistersCacheInitializer_WhenCacheConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(ValidCacheConnectionString));

        Assert.That(services.Any(d => d.ServiceType == typeof(NutritionDbInitializer)), Is.True);
    }

    [Test]
    public void AddNutrition_RegistersCacheCleanupService_WhenCacheConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(ValidCacheConnectionString));

        Assert.That(services.Any(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(NutritionCacheCleanupService)), Is.True);
    }

    [Test]
    public void AddNutrition_DoesNotRegisterCacheInitializer_WhenCacheNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(null));

        Assert.That(services.Any(d => d.ServiceType == typeof(NutritionDbInitializer)), Is.False);
    }

    [Test]
    public void AddNutrition_DoesNotRegisterCacheCleanupService_WhenCacheNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(null));

        Assert.That(services.Any(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(NutritionCacheCleanupService)), Is.False);
    }

    [Test]
    public void AddNutrition_RegistersFatSecretOptions_WhenFatSecretConfigured()
    {
        var services = new ServiceCollection();
        var data = new Dictionary<string, string?>
        {
            ["Nutrition:CacheConnectionString"] = ValidCacheConnectionString,
            ["FatSecret:ConsumerKey"] = "key",
            ["FatSecret:ConsumerSecret"] = "secret",
        };

        services.AddNutrition(new ConfigurationBuilder().AddInMemoryCollection(data).Build());

        using var provider = services.BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IOptions<FatSecretOptions>>().Value.ConsumerKey, Is.EqualTo("key"));
    }

    [Test]
    public void AddNutrition_DoesNotConfigureFatSecretOptions_WhenFatSecretNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(ValidCacheConnectionString));

        using var provider = services.BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IOptions<FatSecretOptions>>().Value.ConsumerKey, Is.Empty);
    }

    private static IConfiguration Configuration(string? cacheConnectionString)
    {
        var data = new Dictionary<string, string?>
        {
            ["Nutrition:CacheConnectionString"] = cacheConnectionString,
        };
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }
}