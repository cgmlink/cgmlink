using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public sealed class MealService : IMealService
{
    private readonly IRepository<MealIngredient> _mealIngredientsRepository;

    public MealService(IRepository<MealIngredient> mealIngredientsRepository)
    {
        _mealIngredientsRepository = mealIngredientsRepository;
    }

    public Meal RecalculateNutrition(Meal meal)
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

    public async Task RecalculateNutritionForIngredient(Guid ingredientId, CancellationToken cancellationToken = default)
    {
        var meals = (await _mealIngredientsRepository.GetAll()
            .Include(mi => mi.Meal)
                .ThenInclude(m => m!.Ingredients)
                    .ThenInclude(mi => mi.Serving)
            .Where(mi => mi.IngredientId == ingredientId && mi.Meal != null && mi.Meal.Deleted == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false))
            .Select(mi => mi.Meal!)
            .Distinct()
            .ToList();

        foreach (var meal in meals)
        {
            RecalculateNutrition(meal);
        }
    }
}