using CgmLink.Nutrition.Endpoints.GetFood;
using CgmLink.Nutrition.Source;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CgmLink.Nutrition.Endpoints.SearchNutrition;

internal static class Endpoint
{
    internal static async Task<Results<Ok<SearchNutritionResponse>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        [AsParameters] SearchNutritionRequest request,
        [FromServices] IValidator<SearchNutritionRequest> validator,
        [FromServices] INutritionSourceClient nutritionSource,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var products = await nutritionSource.SearchAsync(
            request.Query, request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(new SearchNutritionResponse
        {
            Foods = products.Select(GetFoodResponse.ToResponse).ToList(),
        });
    }
}
