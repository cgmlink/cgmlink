using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Meals.GetMeal;

internal static class Endpoint
{
    internal static async Task<Results<Ok<GetMealResponse>, NotFound, UnauthorizedHttpResult>> HandleAsync(
        [FromRoute] Guid id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Meal> mealsRepository,
        [FromServices] IRepository<MealIngredient> mealIngredientsRepository,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.GetUserId();

        var meal = await mealsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.Deleted == null, cancellationToken)
            .ConfigureAwait(false);

        if (meal is null)
        {
            throw new NotFoundException("MEAL_NOT_FOUND");
        }

        var ingredientCount = await mealIngredientsRepository
            .CountAsync(mi => mi.MealId == id, cancellationToken)
            .ConfigureAwait(false);

        return TypedResults.Ok(GetMealResponse.ToResponse(meal, ingredientCount));
    }
}