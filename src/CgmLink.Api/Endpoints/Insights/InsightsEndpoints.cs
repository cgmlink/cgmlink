using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CgmLink.Api.Endpoints.Insights;

[ExcludeFromCodeCoverage]
internal static class InsightsEndpoints
{
    internal static IEndpointRouteBuilder MapInsightsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/insights")
            .WithTags("Insights");

        group.MapGet("/average-glucose", AverageGlucose.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("AverageGlucoseInsight")
            .RequireAuthorization();

        group.MapGet("/time-in-range", TimeInRange.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("TimeInRangeInsight")
            .RequireAuthorization();

        group.MapGet("/hourly-average-glucose", HourlyAverageGlucose.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("HourlyAverageGlucoseInsight")
            .RequireAuthorization();

        group.MapGet("/glucose-variability", GlucoseVariability.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("GlucoseVariabilityInsight")
            .RequireAuthorization();

        group.MapGet("/hypo-events", HypoEvents.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("HypoEventsInsight")
            .RequireAuthorization();

        return endpoints;
    }
}