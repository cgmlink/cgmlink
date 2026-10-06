using FluentValidation;
using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

internal static class Endpoint
{
    internal static async Task<Results<Ok<ListIngredientsResponse>, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [AsParameters] ListIngredientsRequest request,
        [FromServices] IValidator<ListIngredientsRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IIngredientsService ingredientsService,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        ListIngredientsResponse response;
        if (request.Type == IngredientType.External)
        {
            var ingredients = await ingredientsService.SearchExternalIngredientsAsync(
                request.Name!, request.Page, request.PageSize, cancellationToken).ConfigureAwait(false);
            response = new ListIngredientsResponse { Ingredients = ingredients, NumberOfPages = null };
        }
        else
        {
            response = await ingredientsService.ListPersonalIngredientsAsync(userId,
                request.Page, request.PageSize, request.Name, request.SortBy, request.SortDirection, cancellationToken)
                .ConfigureAwait(false);
        }

        return TypedResults.Ok(response);
    }
}
