using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Ingredients.GetIngredient;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GetIngredientResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Ingredient> ingredientsRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var ingredient = await ingredientsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Include(i => i.Servings)
            .FirstOrDefaultAsync(i => i.Id == id && i.Users.Any(u => u.UserId == userId) && i.Deleted == null, cancellationToken)
            .ConfigureAwait(false);

        if (ingredient is null)
        {
            throw new NotFoundException("INGREDIENT_NOT_FOUND");
        }

        return TypedResults.Ok(GetIngredientResponse.ToResponse(ingredient));
    }
}