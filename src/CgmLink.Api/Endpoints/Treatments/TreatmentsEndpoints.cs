using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Api.Endpoints.Treatments;

[ExcludeFromCodeCoverage]
public static class TreatmentsEndpoints
{
    internal static IEndpointRouteBuilder MapTreatmentsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/treatments")
            .WithTags("Treatments");

        group.MapPost("/", NewTreatment.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("CreateTreatment")
            .RequireAuthorization();

        return endpoints;
    }
}