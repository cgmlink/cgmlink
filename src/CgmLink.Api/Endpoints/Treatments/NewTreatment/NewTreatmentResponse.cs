using CgmLink.Data.Entities;
using System;

namespace CgmLink.Api.Endpoints.Treatments.NewTreatment;

public sealed record NewTreatmentResponse
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

    public static NewTreatmentResponse ToResponse(Treatment treatment, Injection? injection)
    {
        return new NewTreatmentResponse
        {
            Id = treatment.Id,
            Created = treatment.Created,
            Updated = treatment.Updated,
            Calories = treatment.Calories,
            Carbs = treatment.Carbs,
            Protein = treatment.Protein,
            Fat = treatment.Fat,
            InsulinName = injection?.Insulin?.Name,
            InsulinUnits = injection?.Units,
        };
    }
}