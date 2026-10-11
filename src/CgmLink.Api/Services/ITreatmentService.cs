using CgmLink.Data.Entities;
using CgmLink.Api.Endpoints.Treatments.GetTreatment;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface ITreatmentService
{
    Task<GetTreatmentResponse> GetTreatmentAsync(Guid treatmentId, Guid userId, CancellationToken cancellationToken = default);

    Treatment CreateTreatment(
        IEnumerable<ITreatmentMealRequest> meals,
        IEnumerable<IMealIngredientRequest> ingredients,
        Dictionary<Guid, Meal> mealLookup,
        Dictionary<Guid, Ingredient> ingredientLookup,
        Guid userId,
        Guid? readingId,
        Guid? injectionId,
        DateTimeOffset created,
        IEnumerable<ResolvedIngredient> nutritionIngredients);

    Task<Treatment> UpdateTreatmentFoodsAsync(
        Treatment treatment,
        IEnumerable<ITreatmentMealRequest>? meals,
        IEnumerable<IMealIngredientRequest>? ingredients,
        IEnumerable<IngredientReference>? nutritionIngredients,
        DateTimeOffset updated,
        CancellationToken cancellationToken = default);

    Task<Treatment> RecalculateTreatmentNutritionAsync(
        Treatment treatment,
        CancellationToken cancellationToken = default);
}
