using CgmLink.Nutrition.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class StartupTests
{
    [Test]
    public void AddNutrition_RegistersCacheInitializer()
    {
        var services = new ServiceCollection();

        services.AddNutrition(_ => { });

        Assert.That(services.Any(d => d.ServiceType == typeof(NutritionDbInitializer)), Is.True);
    }

    [Test]
    public void AddNutrition_RegistersCacheCleanupService()
    {
        var services = new ServiceCollection();

        services.AddNutrition(_ => { });

        Assert.That(services.Any(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
            && d.ImplementationType == typeof(NutritionCacheCleanupService)), Is.True);
    }
}