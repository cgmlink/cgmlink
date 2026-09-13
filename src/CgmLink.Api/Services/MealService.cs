using CgmLink.Data.Entities;

namespace CgmLink.Api.Services;

public sealed class MealService : IMealService
{
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
}