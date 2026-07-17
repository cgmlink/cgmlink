using FluentValidation;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Extensions;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Models;
using UnitOfMeasurement = CgmLink.Api.Models.UnitOfMeasurement;

namespace CgmLink.Api.Endpoints.Food.List;

internal static class Endpoint
{
    internal static async Task<Results<Ok<ListFoodResponse>, UnauthorizedHttpResult, ValidationProblem>> HandleAsync(
        [AsParameters] ListFoodRequest request,
        [FromServices] IValidator<ListFoodRequest> validator,
        [FromServices] ICurrentUser currentUser,
        [FromServices] IRepository<Meal> mealRepository,
        [FromServices] IRepository<Ingredient> ingredientRepository,
        CancellationToken cancellationToken)
    {
        if (await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false) is
            { IsValid: false } validation)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userId = currentUser.GetUserId();

        var mealsQuery = mealRepository.Find(m => m.UserId == userId, new FindOptions { IsAsNoTracking = true });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
#pragma warning disable CA1862
            mealsQuery = mealsQuery.Where(m => m.Name.ToLower().Contains(searchLower));
#pragma warning restore CA1862
        }

        var meals = await mealsQuery
            .Include(m => m.MealIngredients)
                .ThenInclude(mi => mi.Ingredient)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var ingredientsQuery = ingredientRepository.Find(i => i.UserId == userId, new FindOptions { IsAsNoTracking = true });

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLowerInvariant();
#pragma warning disable CA1862
            ingredientsQuery = ingredientsQuery.Where(i => i.Name.ToLower().Contains(searchLower));
#pragma warning restore CA1862
        }

        var ingredients = await ingredientsQuery
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var food = meals.Select(meal => new FoodResponse
        {
            Id = meal.Id,
            Name = meal.Name,
            FoodType = FoodType.Meal,
            Created = meal.Created,
            Updated = meal.Updated,
            TotalCalories = meal.MealIngredients.Sum(mi => mi.Ingredient == null ? 0 : mi.Ingredient.Calories * mi.Quantity),
            TotalCarbs = meal.MealIngredients.Sum(mi => mi.Ingredient == null ? 0 : mi.Ingredient.Carbs * mi.Quantity),
            TotalProtein = meal.MealIngredients.Sum(mi => mi.Ingredient == null ? 0 : mi.Ingredient.Protein * mi.Quantity),
            TotalFat = meal.MealIngredients.Sum(mi => mi.Ingredient == null ? 0 : mi.Ingredient.Fat * mi.Quantity),
            Ingredients = meal.MealIngredients.Select(mi => new FoodIngredientResponse
            {
                Id = mi.Ingredient?.Id ?? Guid.Empty,
                Name = mi.Ingredient?.Name ?? string.Empty,
                Quantity = mi.Quantity,
                Carbs = mi.Ingredient?.Carbs ?? 0,
                Protein = mi.Ingredient?.Protein ?? 0,
                Fat = mi.Ingredient?.Fat ?? 0,
                Calories = mi.Ingredient?.Calories ?? 0,
                Uom = (UnitOfMeasurement)(mi.Ingredient?.Uom ?? Data.Enums.UnitOfMeasurement.Unit)
            }).ToList()
        }).Union(ingredients.Select(ingredient => new FoodResponse
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            FoodType = FoodType.Ingredient,
            Created = ingredient.Created,
            Updated = ingredient.Updated,
            TotalCalories = ingredient.Calories,
            TotalCarbs = ingredient.Carbs,
            TotalProtein = ingredient.Protein,
            TotalFat = ingredient.Fat,
            Barcode = ingredient.Barcode,
            Uom = (UnitOfMeasurement)ingredient.Uom,
            Ingredients = []
        })).ToList();

        var sortValues = request.GetSortValues();
        var descending = request.IsDescending();

        IOrderedEnumerable<FoodResponse> ordered = food.OrderByProperty(SortingMapping.SortPropertyMap[sortValues[0]], descending);

        foreach (var sortValue in sortValues.Skip(1))
        {
            ordered = ordered.ThenByProperty(SortingMapping.SortPropertyMap[sortValue], descending);
        }

        var sortedFood = ordered.ToList();

        var totalCount = sortedFood.Count;
        var pagedFood = sortedFood
            .Skip(request.Page * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var numberOfPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

        var response = new ListFoodResponse
        {
            NumberOfPages = numberOfPages,
            Food = pagedFood
        };

        return TypedResults.Ok(response);
    }
}
