using CgmLink.Data.Entities;
using System;

namespace CgmLink.Api.Endpoints.Treatments.UpdateTreatment;

public sealed record UpdateTreatmentResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }
    public string? InsulinName { get; init; }
    public decimal? InsulinUnits { get; init; }
    public required int MealCount { get; init; }
    public required int IngredientCount { get; init; }

    public static UpdateTreatmentResponse ToResponse(Treatment treatment)
    {
        return new UpdateTreatmentResponse
        {
            Id = treatment.Id,
            Created = treatment.Created,
            Updated = treatment.Updated,
            Calories = treatment.Calories,
            Carbs = treatment.Carbs,
            Protein = treatment.Protein,
            Fat = treatment.Fat,
            InsulinName = treatment.Injection?.Insulin?.Name,
            InsulinUnits = treatment.Injection?.Units,
            MealCount = treatment.Meals.Count,
            IngredientCount = treatment.Ingredients.Count,
        };
    }
}
