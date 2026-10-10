using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Resources;
using CgmLink.Nutrition;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public sealed class MealService : IMealService
{
    private readonly IRepository<Meal> _mealsRepository;
    private readonly INutritionCatalog _nutritionCatalog;

    public MealService(IRepository<Meal> mealsRepository, INutritionCatalog nutritionCatalog)
    {
        _mealsRepository = mealsRepository;
        _nutritionCatalog = nutritionCatalog;
    }

    public async Task<Meal> RecalculateMealsNutritionAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        var calories = 0m;
        var carbs = 0m;
        var protein = 0m;
        var fat = 0m;

        foreach (var mealIngredient in meal.Ingredients)
        {
            var serving = mealIngredient.Serving;
            if (serving is null)
            {
                continue;
            }

            calories += serving.Calories * mealIngredient.Quantity;
            carbs += serving.Carbs * mealIngredient.Quantity;
            protein += serving.Protein * mealIngredient.Quantity;
            fat += serving.Fat * mealIngredient.Quantity;
        }

        var products = new Dictionary<string, Nutrition.Source.NutritionProduct>();
        foreach (var mealIngredient in meal.NutritionIngredients)
        {
            var ingredient = mealIngredient.NutritionIngredient;
            if (ingredient is null || ingredient.Source != _nutritionCatalog.Source || mealIngredient.Serving is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            if (!products.TryGetValue(ingredient.ProductId, out var product))
            {
                product = await _nutritionCatalog.GetAsync(ingredient.ProductId, cancellationToken).ConfigureAwait(false)
                    ?? throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
                products.Add(ingredient.ProductId, product);
            }

            var serving = product.Servings.SingleOrDefault(s => s.ExternalId == mealIngredient.Serving.ServingId)
                ?? throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            calories += serving.Calories * mealIngredient.Quantity;
            carbs += serving.Carbs * mealIngredient.Quantity;
            protein += serving.Protein * mealIngredient.Quantity;
            fat += serving.Fat * mealIngredient.Quantity;
        }

        meal.Calories = calories;
        meal.Carbs = carbs;
        meal.Protein = protein;
        meal.Fat = fat;

        return meal;
    }

    public async Task RecalculateMealsWithIngredientNutrition(Guid ingredientId, CancellationToken cancellationToken = default)
    {
        var meals = await _mealsRepository.GetAll()
            .AsSplitQuery()
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Serving)
            .Include(m => m.NutritionIngredients)
                .ThenInclude(mi => mi.NutritionIngredient)
            .Include(m => m.NutritionIngredients)
                .ThenInclude(mi => mi.Serving)
            .Where(m => m.Deleted == null && m.Ingredients.Any(mi => mi.IngredientId == ingredientId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var meal in meals)
        {
            await RecalculateMealsNutritionAsync(meal, cancellationToken).ConfigureAwait(false);
        }
    }

    public void UpdateMealsIngredients(
        Meal meal,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Ingredient> ingredientLookup)
    {
        var requestIngredients = ingredients.ToList();
        var currentIngredients = meal.Ingredients.ToList();
        var currentByIngredientId = currentIngredients.ToDictionary(mi => mi.IngredientId);
        var requestedIds = requestIngredients.Select(i => i.IngredientId).ToHashSet();

        foreach (var mealIngredient in requestIngredients)
        {
            var ingredient = ingredientLookup[mealIngredient.IngredientId];
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
    }

    public async Task<Dictionary<Guid, Meal>> GetValidatedMealsAsync(
        IEnumerable<ITreatmentMealRequest> meals,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var requestMeals = meals.ToList();
        var mealIds = requestMeals.Select(m => m.MealId).Distinct().ToList();

        var mealLookup = await _mealsRepository.GetAll()
            .Where(m => mealIds.Contains(m.Id) && m.UserId == userId && m.Deleted == null)
            .ToDictionaryAsync(m => m.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var meal in requestMeals)
        {
            if (!mealLookup.ContainsKey(meal.MealId))
            {
                throw new BadRequestException(ValidationMessages.MealIdInvalid);
            }
        }

        return mealLookup;
    }
}