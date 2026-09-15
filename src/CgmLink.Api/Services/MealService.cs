using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
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

    public MealService(IRepository<Meal> mealsRepository)
    {
        _mealsRepository = mealsRepository;
    }

    public Meal RecalculateMealsNutrition(Meal meal)
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

        meal.Calories = calories;
        meal.Carbs = carbs;
        meal.Protein = protein;
        meal.Fat = fat;

        return meal;
    }

    public async Task RecalculateMealsWithIngredientNutrition(Guid ingredientId, CancellationToken cancellationToken = default)
    {
        var meals = await _mealsRepository.GetAll()
            .Include(m => m.Ingredients)
                .ThenInclude(mi => mi.Serving)
            .Where(m => m.Deleted == null && m.Ingredients.Any(mi => mi.IngredientId == ingredientId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var meal in meals)
        {
            RecalculateMealsNutrition(meal);
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
}