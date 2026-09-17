using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;

namespace CgmLink.Api.Services;

public interface ITreatmentService
{
    Treatment CreateTreatment(
        IEnumerable<ITreatmentMealRequest> meals,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Meal> mealLookup,
        Dictionary<Guid, Ingredient> ingredientLookup,
        Guid userId,
        Guid? readingId,
        Guid? injectionId,
        DateTimeOffset created);

    Treatment RecalculateTreatmentNutrition(Treatment treatment);
}