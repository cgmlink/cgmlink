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

namespace CgmLink.Api.Endpoints.Meals.GetMeal;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GetMealResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Meal> mealsRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var meal = await mealsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .Where(m => m.Id == id && m.UserId == userId && m.Deleted == null)
            .Select(m => GetMealResponse.ToResponse(m, m.Ingredients.Count))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (meal is null)
        {
            throw new NotFoundException("MEAL_NOT_FOUND");
        }

        return TypedResults.Ok(meal);
    }
}