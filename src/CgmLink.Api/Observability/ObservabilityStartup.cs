using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
#if ENABLE_PRIVATE_OBSERVABILITY
using CgmLink.SystemsApi.Observability.DependencyInjection;
using CgmLink.SystemsApi.Observability.Logging;
#endif

namespace CgmLink.Api.Observability;

internal static class ObservabilityStartup
{
    public static void AddObservability(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction())
        {
            return;
        }
#if ENABLE_PRIVATE_OBSERVABILITY
        var section = builder.Configuration.GetSection("Observability");
        if (!section.GetValue<bool>("Enabled"))
        {
            return;
        }

        var baseUrl = section["BaseUrl"];
        var apiKey = section["ApiKey"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Observability:BaseUrl must be configured when production observability is enabled.");
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Observability:ApiKey must be configured when production observability is enabled.");
        }

        var serviceName = section["ServiceName"];
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            serviceName = "CgmLink.Api";
        }

        var environment = builder.Environment.EnvironmentName;
        var serviceInstanceId = Environment.MachineName;

        builder.Logging.AddCgmLinkObservability(options =>
        {
            options.BaseUrl = baseUrl;
            options.ApiKey = apiKey;
            options.ServiceName = serviceName;
            options.Environment = environment;
            options.ServiceInstanceId = serviceInstanceId;

            BindOptionalInteger(section, "LogQueueCapacity", value => options.LogQueueCapacity = value);
            BindOptionalInteger(section, "LogBatchSize", value => options.LogBatchSize = value);
            BindOptionalInteger(section, "LogFlushIntervalMilliseconds",
                value => options.LogFlushIntervalMilliseconds = value);
        });

        builder.Services.AddObservabilitySdk(options =>
        {
            options.BaseUrl = baseUrl;
            options.ApiKey = apiKey;
            options.ServiceName = serviceName;
            options.Environment = environment;
            options.ServiceInstanceId = serviceInstanceId;
            options.MeterNames = [serviceName];
        });

        builder.Services.AddObservabilityHealthChecks();
#endif
    }

#if ENABLE_PRIVATE_OBSERVABILITY
    private static void BindOptionalInteger(IConfigurationSection section, string key, Action<int> apply)
    {
        var value = section.GetValue<int?>(key);
        if (value.HasValue)
        {
            apply(value.Value);
        }
    }
#endif
}
