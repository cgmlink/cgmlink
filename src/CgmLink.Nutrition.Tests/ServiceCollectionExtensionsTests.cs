using System.Collections.Generic;
using CgmLink.Nutrition;
using CgmLink.Nutrition.Caching;
using CgmLink.Nutrition.Caching.Ef;
using CgmLink.Nutrition.FatSecretClient;
using CgmLink.Nutrition.Source;
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

        Assert.That(services.Any(d => d.ServiceType == typeof(INutritionDbInitializer)), Is.True);
    }

    [Test]
    public void AddNutrition_RegistersCacheCleanupService_WhenCacheConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(ValidCacheConnectionString));

        Assert.That(services.Any(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(SqlCacheCleanupService)), Is.True);
    }

    [Test]
    public void AddNutrition_DoesNotRegisterCacheInitializer_WhenCacheNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(null));

        Assert.That(services.Any(d => d.ServiceType == typeof(INutritionDbInitializer)), Is.False);
    }

    [Test]
    public void AddNutrition_DoesNotRegisterCacheCleanupService_WhenCacheNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(null));

        Assert.That(services.Any(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(SqlCacheCleanupService)), Is.False);
    }

    [Test]
    public void AddNutrition_RegistersNutritionCache_WhenCacheConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(ValidCacheConnectionString));

        Assert.That(services.Any(d => d.ServiceType == typeof(INutritionCache)
            && d.ImplementationType == typeof(SqlServerNutritionCache)), Is.True);
    }

    [Test]
    public void AddNutrition_DoesNotRegisterNutritionCache_WhenCacheNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(null));

        Assert.That(services.Any(d => d.ServiceType == typeof(INutritionCache)), Is.False);
    }

    [Test]
    public void AddNutrition_RegistersFatSecretOptions_WhenFatSecretConfigured()
    {
        var services = new ServiceCollection();
        var data = new Dictionary<string, string?>
        {
            ["Nutrition:CacheConnectionString"] = ValidCacheConnectionString,
            ["FatSecret:ClientId"] = "client-id",
            ["FatSecret:ClientSecret"] = "secret",
        };

        services.AddNutrition(new ConfigurationBuilder().AddInMemoryCollection(data).Build());

        using var provider = services.BuildServiceProvider();
        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IOptions<FatSecretOptions>>().Value.ClientId, Is.EqualTo("client-id"));
            Assert.That(provider.GetServices<INutritionSourceClient>().Single().Source, Is.EqualTo("fatsecret"));
        });
    }

    [Test]
    public void AddNutrition_RegistersFatSecretClient_WhenCacheNotConfigured()
    {
        var services = new ServiceCollection();
        var data = new Dictionary<string, string?>
        {
            ["FatSecret:ClientId"] = "client-id",
            ["FatSecret:ClientSecret"] = "secret",
        };

        services.AddNutrition(new ConfigurationBuilder().AddInMemoryCollection(data).Build());

        Assert.That(services.Any(d => d.ServiceType == typeof(INutritionSourceClient)), Is.True);
    }

    [Test]
    public void AddNutrition_DoesNotConfigureFatSecretOptions_WhenFatSecretNotConfigured()
    {
        var services = new ServiceCollection();

        services.AddNutrition(Configuration(ValidCacheConnectionString));

        using var provider = services.BuildServiceProvider();
        Assert.That(provider.GetRequiredService<IOptions<FatSecretOptions>>().Value.ClientId, Is.Empty);
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
