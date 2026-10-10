using CgmLink.Data.Entities;
using CgmLink.Api.Endpoints.Meals.GetMeal;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IMealService
{
    Task<GetMealResponse> GetMealAsync(Guid mealId, Guid userId, CancellationToken cancellationToken = default);
    Task<Meal> RecalculateMealsNutritionAsync(Meal meal, CancellationToken cancellationToken = default);
    Task RecalculateMealsWithIngredientNutrition(Guid ingredientId, CancellationToken cancellationToken = default);
    Task UpdateMealsIngredientsAsync(
        Meal meal,
        IEnumerable<IMealIngredientRequest>? ingredients,
        IEnumerable<IMealNutritionIngredientRequest>? nutritionIngredients,
        CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, Meal>> GetValidatedMealsAsync(
        IEnumerable<ITreatmentMealRequest> meals,
        Guid userId,
        CancellationToken cancellationToken = default);
}
