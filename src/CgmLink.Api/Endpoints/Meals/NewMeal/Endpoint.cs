using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using CgmLink.Resources;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Meals.NewMeal;

internal static class Endpoint
{
    internal static async Task<Results<Created<NewMealResponse>, ValidationProblem>> HandleAsync(
        [FromBody] NewMealRequest request,
        [FromServices] IValidator<NewMealRequest> validator,
        [FromServices] IRepository<Meal> mealsRepository,
        [FromServices] IRepository<Ingredient> ingredientsRepository,
        [FromServices] IRepository<User> usersRepository,
        [FromServices] ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();
        var user = await usersRepository.FindOneAsync(u => u.Id == userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            throw new UnauthorizedException("USER_NOT_LOGGED_IN", UnauthorizedSource.CgmLink);
        }

        var ingredientIds = request.Ingredients.Select(i => i.IngredientId).Distinct().ToList();
        var servingIds = request.Ingredients.Select(i => i.ServingId).Distinct().ToList();

        var ingredients = await ingredientsRepository.GetAll()
            .Where(i => ingredientIds.Contains(i.Id) && i.Users.Any(u => u.UserId == userId) && i.Deleted == null)
            .Include(i => i.Servings.Where(s => servingIds.Contains(s.Id)))
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

        var calories = 0m;
        var carbs = 0m;
        var protein = 0m;
        var fat = 0m;
        foreach (var mealIngredient in request.Ingredients)
        {
            var ingredient = ingredients[mealIngredient.IngredientId];
            var serving = ingredient.Servings.Single(s => s.Id == mealIngredient.ServingId);
            calories += serving.Calories * mealIngredient.Quantity;
            carbs += serving.Carbs * mealIngredient.Quantity;
            protein += serving.Protein * mealIngredient.Quantity;
            fat += serving.Fat * mealIngredient.Quantity;
        }

        var meal = new Meal
        {
            Name = request.Name,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            UserId = userId,
            Calories = calories,
            Carbs = carbs,
            Protein = protein,
            Fat = fat,
            Created = DateTimeOffset.UtcNow,
        };

        foreach (var mealIngredient in request.Ingredients)
        {
            meal.Ingredients.Add(new MealIngredient
            {
                MealId = meal.Id,
                IngredientId = mealIngredient.IngredientId,
                ServingId = mealIngredient.ServingId,
                Quantity = mealIngredient.Quantity,
                Created = DateTimeOffset.UtcNow,
            });
        }

        await mealsRepository.AddAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.Created($"/api/v1/meals/{meal.Id}", NewMealResponse.ToResponse(meal));
    }
}