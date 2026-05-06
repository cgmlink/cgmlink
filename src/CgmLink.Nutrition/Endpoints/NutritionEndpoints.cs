using System.Diagnostics.CodeAnalysis;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Nutrition.Endpoints.GetProduct;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CgmLink.Nutrition.Endpoints;

[ExcludeFromCodeCoverage]
internal static class NutritionEndpoints
{
    internal static IEndpointRouteBuilder MapNutritionEndpointsInternal(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/nutrition").WithTags("Nutrition");

        group.MapGet("/products/{code}", GetProduct.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .Produces<ProductResponse>()
            .RequireAuthorization();

        group.MapGet("/products/search/{term}", SearchProduct.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .Produces<IEnumerable<ProductResponse>>()
            .RequireAuthorization();

        return endpoints;
    }
}