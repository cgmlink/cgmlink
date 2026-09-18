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

namespace CgmLink.Api.Endpoints.Meals.DeleteMeal;

internal static class Endpoint
{
    internal static async Task<Results<NoContent, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Meal> mealsRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var meal = await mealsRepository.GetAll()
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, cancellationToken)
            .ConfigureAwait(false);

        if (meal is null || meal.Deleted is not null)
        {
            throw new NotFoundException("MEAL_NOT_FOUND");
        }

        meal.Deleted = DateTimeOffset.UtcNow;

        await mealsRepository.UpdateAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.NoContent();
    }
}