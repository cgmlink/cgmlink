using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Api.Endpoints.Food;

[ExcludeFromCodeCoverage]
public static class FoodEndpoints
{
    internal static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/food")
            .WithTags("Food");

        group.MapGet("/", List.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("ListFood")
            .RequireAuthorization();

        return endpoints;
    }
}
