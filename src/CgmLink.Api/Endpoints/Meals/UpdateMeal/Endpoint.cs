using FluentValidation;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using CgmLink.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
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
        [FromServices] IRepository<Ingredient> ingredientsRepository,
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
            var ingredientIds = request.Ingredients.Select(i => i.IngredientId).Distinct().ToList();

            var ingredients = await ingredientsRepository.GetAll()
                .Where(i => ingredientIds.Contains(i.Id) && i.Users.Any(u => u.UserId == userId) && i.Deleted == null)
                .Include(i => i.Servings)
                .ToDictionaryAsync(i => i.Id, cancellationToken)
                .ConfigureAwait(false);

            foreach (var mealIngredient in request.Ingredients)
            {
                if (!ingredients.TryGetValue(mealIngredient.IngredientId, out var ingredient) ||
                    !ingredient.Servings.Any(s => s.Id == mealIngredient.ServingId))
                {
                    throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
                }
            }

            meal.Ingredients.Clear();
            foreach (var mealIngredient in request.Ingredients)
            {
                var ingredient = ingredients[mealIngredient.IngredientId];
                meal.Ingredients.Add(new MealIngredient
                {
                    MealId = meal.Id,
                    IngredientId = mealIngredient.IngredientId,
                    ServingId = mealIngredient.ServingId,
                    Serving = ingredient.Servings.Single(s => s.Id == mealIngredient.ServingId),
                    Quantity = mealIngredient.Quantity,
                    Created = DateTimeOffset.UtcNow,
                });
            }
        }
        meal.Updated = DateTimeOffset.UtcNow;

        mealService.RecalculateNutrition(meal);

        await mealsRepository.UpdateAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UpdateMealResponse.ToResponse(meal, meal.Ingredients.Count));
    }
}