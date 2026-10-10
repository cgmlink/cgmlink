using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IMealService
{
    Task<Meal> RecalculateMealsNutritionAsync(Meal meal, CancellationToken cancellationToken = default);
    Task RecalculateMealsWithIngredientNutrition(Guid ingredientId, CancellationToken cancellationToken = default);
    Task UpdateMealsIngredientsAsync(
        Meal meal,
        IEnumerable<IMealIngredientRequest> ingredients,
        CancellationToken cancellationToken = default);
    Task UpdateMealsNutritionIngredientsAsync(
        Meal meal,
        IEnumerable<IMealNutritionIngredientRequest> ingredients,
        CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, Meal>> GetValidatedMealsAsync(
        IEnumerable<ITreatmentMealRequest> meals,
        Guid userId,
        CancellationToken cancellationToken = default);
}
