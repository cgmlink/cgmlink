using CgmLink.Api.Services;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.GetIngredient;

internal static class Endpoint
{
    internal static async Task<Results<Ok<IngredientResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] string identifier,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IIngredientsService ingredientsService,
        CancellationToken cancellationToken,
        [FromQuery] IngredientType type = IngredientType.Personal)
    {
        var response = await ingredientsService.GetIngredientAsync(identifier, currentUser.GetUserId(), cancellationToken, type)
            .ConfigureAwait(false);
        return TypedResults.Ok(response);
    }
}
