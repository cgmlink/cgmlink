using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Treatments.ListTreatments;

public sealed record ListTreatmentsResponse : PagedResponse
{
    public required ICollection<ListTreatmentResponse> Treatments { get; init; } = [];
}

public sealed record ListTreatmentResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }
    public double? GlucoseLevel { get; init; }
    public decimal? InjectionUnits { get; init; }
    public string? InsulinName { get; init; }

    public static ListTreatmentResponse ToResponse(Treatment treatment)
    {
        return new ListTreatmentResponse
        {
            Id = treatment.Id,
            Created = treatment.Created,
            Updated = treatment.Updated,
            Calories = treatment.Calories,
            Carbs = treatment.Carbs,
            Protein = treatment.Protein,
            Fat = treatment.Fat,
            GlucoseLevel = treatment.Reading?.GlucoseLevel,
            InjectionUnits = treatment.Injection?.Units,
            InsulinName = treatment.Injection?.Insulin?.Name,
        };
    }
}