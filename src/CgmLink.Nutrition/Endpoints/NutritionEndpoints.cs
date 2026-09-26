using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Nutrition.Endpoints;

[ExcludeFromCodeCoverage]
internal static class NutritionEndpoints
{
    internal static IEndpointRouteBuilder MapNutritionEndpointsInternal(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/nutrition")
            .WithTags("Nutrition");

        group.MapGet("/search", SearchFood.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("SearchFood")
            .RequireAuthorization();

        group.MapGet("/foods/barcode/{barcode}", GetFoodByBarcode.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("GetFoodByBarcode")
            .RequireAuthorization();

        group.MapGet("/foods/{productId}", GetFood.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("GetFood")
            .RequireAuthorization();

        return endpoints;
    }
}
