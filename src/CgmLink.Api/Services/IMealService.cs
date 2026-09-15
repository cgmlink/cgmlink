using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IMealService
{
    Meal RecalculateMealsNutrition(Meal meal);
    Task RecalculateMealsWithIngredientNutrition(Guid ingredientId, CancellationToken cancellationToken = default);
    void UpdateMealsIngredients(
        Meal meal,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Ingredient> ingredientLookup);
}