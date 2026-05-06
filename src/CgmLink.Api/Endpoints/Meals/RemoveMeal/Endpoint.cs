using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Meals.RemoveMeal;

internal static class Endpoint
{
    internal static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] IRepository<Meal> mealRepository,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var meal = await mealRepository.FindOneAsync(m => m.Id == id && m.UserId == userId, new FindOptions { IsAsNoTracking = true }, cancellationToken).ConfigureAwait(false);

        if (meal is null)
        {
            throw new NotFoundException("MEAL_NOT_FOUND");
        }

        await mealRepository.DeleteAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}
