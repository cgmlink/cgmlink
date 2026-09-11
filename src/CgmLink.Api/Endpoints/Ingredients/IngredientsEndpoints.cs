using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Api.Endpoints.Ingredients;

[ExcludeFromCodeCoverage]
public static class IngredientsEndpoints
{
    internal static IEndpointRouteBuilder MapIngredientsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/ingredients")
            .WithTags("Ingredients");

        group.MapGet("/", ListIngredients.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("ListIngredients")
            .RequireAuthorization();

        group.MapPost("/", NewIngredient.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("CreateIngredient")
            .RequireAuthorization();

        return endpoints;
    }
}