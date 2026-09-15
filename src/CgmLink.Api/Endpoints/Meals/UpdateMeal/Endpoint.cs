using FluentValidation;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Services;
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

namespace CgmLink.Api.Endpoints.Meals.UpdateMeal;

internal static class Endpoint
{
    internal static async Task<Results<Ok<UpdateMealResponse>, NotFound, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [FromRoute] Guid id,
        [FromBody] UpdateMealRequest request,
        [FromServices] IValidator<UpdateMealRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Meal> mealsRepository,
        [FromServices] IIngredientsService ingredientsService,
        [FromServices] IMealService mealService,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();

        var meal = await mealsRepository.GetAll()
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Ingredient)
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Serving)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId && m.Deleted == null, cancellationToken)
            .ConfigureAwait(false);

        if (meal is null)
        {
            throw new NotFoundException("MEAL_NOT_FOUND");
        }

        if (request.Name is not null)
        {
            meal.Name = request.Name;
        }
        if (request.ImageUrl is not null)
        {
            meal.ImageUrl = request.ImageUrl;
        }
        if (request.ThumbnailUrl is not null)
        {
            meal.ThumbnailUrl = request.ThumbnailUrl;
        }

        if (request.Ingredients is not null)
        {
            var ingredientLookup = await ingredientsService
                .GetValidatedIngredientsAsync(request.Ingredients, userId, cancellationToken)
                .ConfigureAwait(false);

            mealService.UpdateMealsIngredients(meal, request.Ingredients, ingredientLookup);
            mealService.RecalculateMealsNutrition(meal);
        }

        meal.Updated = DateTimeOffset.UtcNow;

        await mealsRepository.UpdateAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UpdateMealResponse.ToResponse(meal));
    }
}