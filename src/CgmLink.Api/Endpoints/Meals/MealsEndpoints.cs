using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Diagnostics.CodeAnalysis;

namespace CgmLink.Api.Endpoints.Meals;

[ExcludeFromCodeCoverage]
public static class MealsEndpoints
{
    internal static IEndpointRouteBuilder MapMealsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.NewVersionedApi().MapGroup("api/v{version:apiVersion}/meals")
            .WithTags("Meals");

        group.MapGet("/", List.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("ListMeals")
            .RequireAuthorization();

        group.MapGet("/{id:guid}", GetMeal.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("GetMeal")
            .RequireAuthorization();

        group.MapGet("/{id:guid}/ingredients", ListMealIngredients.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("ListMealIngredients")
            .RequireAuthorization();

        group.MapPost("/", NewMeal.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("CreateMeal")
            .RequireAuthorization();

        group.MapDelete("/{id:guid}", DeleteMeal.Endpoint.HandleAsync)
            .HasApiVersion(1.0)
            .WithName("DeleteMeal")
            .RequireAuthorization();

        return endpoints;
    }
}