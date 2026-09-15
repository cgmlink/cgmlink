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
            var ingredientIds = request.Ingredients.Select(i => i.IngredientId).Distinct().ToList();
            var servingIds = request.Ingredients.Select(i => i.ServingId).Distinct().ToList();

            var ingredients = await ingredientsRepository.GetAll()
                .Where(i => ingredientIds.Contains(i.Id) && i.Users.Any(u => u.UserId == userId) && i.Deleted == null)
                .Include(i => i.Servings.Where(s => servingIds.Contains(s.Id) && s.Deleted == null))
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

            var currentIngredients = meal.Ingredients.ToList();
            var currentByIngredientId = currentIngredients.ToDictionary(mi => mi.IngredientId);
            var requestedIds = request.Ingredients.Select(i => i.IngredientId).ToHashSet();

            foreach (var mealIngredient in request.Ingredients)
            {
                var ingredient = ingredients[mealIngredient.IngredientId];
                if (currentByIngredientId.TryGetValue(mealIngredient.IngredientId, out var existing))
                {
                    existing.ServingId = mealIngredient.ServingId;
                    existing.Serving = ingredient.Servings.Single(s => s.Id == mealIngredient.ServingId);
                    existing.Quantity = mealIngredient.Quantity;
                }
                else
                {
                    meal.Ingredients.Add(new MealIngredient
                    {
                        MealId = meal.Id,
                        IngredientId = mealIngredient.IngredientId,
                        ServingId = mealIngredient.ServingId,
                        Serving = ingredient.Servings.Single(s => s.Id == mealIngredient.ServingId),
                        Ingredient = ingredient,
                        Quantity = mealIngredient.Quantity,
                        Created = DateTimeOffset.UtcNow,
                    });
                }
            }

            foreach (var removed in currentIngredients.Where(mi => !requestedIds.Contains(mi.IngredientId)).ToList())
            {
                meal.Ingredients.Remove(removed);
            }

            mealService.RecalculateNutrition(meal);
        }

        meal.Updated = DateTimeOffset.UtcNow;

        await mealsRepository.UpdateAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.Ok(UpdateMealResponse.ToResponse(meal));
    }
}