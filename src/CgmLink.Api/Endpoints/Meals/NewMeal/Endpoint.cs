using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Endpoints.Meals.NewMeal;

internal static class Endpoint
{
    internal static async Task<Results<Created<NewMealResponse>, ValidationProblem>> HandleAsync(
        [FromBody] NewMealRequest request,
        [FromServices] IValidator<NewMealRequest> validator,
        [FromServices] IRepository<Meal> mealsRepository,
        [FromServices] IIngredientsService ingredientsService,
        [FromServices] IRepository<User> usersRepository,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IMealService mealService,
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

        var ingredientLookup = await ingredientsService
            .GetValidatedIngredientsAsync(request.Ingredients, userId, cancellationToken)
            .ConfigureAwait(false);

        var meal = new Meal
        {
            Name = request.Name,
            ImageUrl = request.ImageUrl,
            ThumbnailUrl = request.ThumbnailUrl,
            UserId = userId,
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Created = DateTimeOffset.UtcNow,
        };

        mealService.UpdateMealsIngredients(meal, request.Ingredients, ingredientLookup);
        mealService.RecalculateMealsNutrition(meal);

        await mealsRepository.AddAsync(meal, cancellationToken).ConfigureAwait(false);

        return TypedResults.Created($"/api/v1/meals/{meal.Id}", NewMealResponse.ToResponse(meal));
    }
}