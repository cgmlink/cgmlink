using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.SearchIngredients;

internal static class Endpoint
{
    internal static async Task<Results<Ok<SearchIngredientsResponse>, ValidationProblem, UnauthorizedHttpResult>> HandleAsync(
        [AsParameters] SearchIngredientsRequest request,
        [FromServices] IValidator<SearchIngredientsRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IIngredientsService ingredientsService,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();
        var validation = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var response = await ingredientsService.SearchIngredientsAsync(
            request.Name, userId, request.Page, request.PageSize, request.IncludeExternal, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(response);
    }
}
