using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using ReadingDirection = CgmLink.Api.Models.ReadingDirection;

namespace CgmLink.Api.Endpoints.Treatments.GetTreatment;

public sealed record GetTreatmentResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }
    public GetTreatmentReadingResponse? Reading { get; init; }
    public string? InsulinName { get; init; }
    public decimal? InsulinUnits { get; init; }
    public required ICollection<GetTreatmentMealResponse> Meals { get; init; } = [];
    public required ICollection<GetTreatmentIngredientResponse> Ingredients { get; init; } = [];

    public static GetTreatmentResponse ToResponse(Treatment treatment)
    {
        return new GetTreatmentResponse
        {
            Id = treatment.Id,
            Created = treatment.Created,
            Updated = treatment.Updated,
            Calories = treatment.Calories,
            Carbs = treatment.Carbs,
            Protein = treatment.Protein,
            Fat = treatment.Fat,
            Reading = treatment.Reading is null ? null : GetTreatmentReadingResponse.ToResponse(treatment.Reading),
            InsulinName = treatment.Injection?.Insulin?.Name,
            InsulinUnits = treatment.Injection?.Units,
            Meals = treatment.Meals.Select(GetTreatmentMealResponse.ToResponse).ToList(),
            Ingredients = treatment.Ingredients.Select(GetTreatmentIngredientResponse.ToResponse).ToList(),
        };
    }
}

public sealed record GetTreatmentReadingResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required double GlucoseLevel { get; init; }
    public required ReadingDirection Direction { get; init; }

    public static GetTreatmentReadingResponse ToResponse(Reading reading)
    {
        return new GetTreatmentReadingResponse
        {
            Id = reading.Id,
            Created = reading.Created,
            GlucoseLevel = reading.GlucoseLevel,
            Direction = (ReadingDirection)reading.Direction,
        };
    }
}

public sealed record GetTreatmentMealResponse
{
    public required Guid MealId { get; init; }
    public required string Name { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }

    public static GetTreatmentMealResponse ToResponse(TreatmentMeal treatmentMeal)
    {
        var meal = treatmentMeal.Meal;

        return new GetTreatmentMealResponse
        {
            MealId = treatmentMeal.MealId,
            Name = meal?.Name ?? string.Empty,
            Quantity = treatmentMeal.Quantity,
            Calories = meal is null ? 0m : meal.Calories * treatmentMeal.Quantity,
            Carbs = meal is null ? 0m : meal.Carbs * treatmentMeal.Quantity,
            Protein = meal is null ? 0m : meal.Protein * treatmentMeal.Quantity,
            Fat = meal is null ? 0m : meal.Fat * treatmentMeal.Quantity,
        };
    }
}

public sealed record GetTreatmentIngredientResponse
{
    public required Guid IngredientId { get; init; }
    public required string IngredientName { get; init; }
    public string? Barcode { get; init; }
    public string? ProductId { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public GetTreatmentServingResponse? Serving { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }

    public static GetTreatmentIngredientResponse ToResponse(TreatmentIngredient treatmentIngredient)
    {
        var ingredient = treatmentIngredient.Ingredient;
        var serving = treatmentIngredient.Serving;

        return new GetTreatmentIngredientResponse
        {
            IngredientId = treatmentIngredient.IngredientId,
            IngredientName = ingredient?.Name ?? string.Empty,
            Barcode = ingredient?.Barcode,
            ProductId = ingredient?.ProductId,
            ImageUrl = ingredient?.ImageUrl,
            ThumbnailUrl = ingredient?.ThumbnailUrl,
            Serving = serving is null ? null : GetTreatmentServingResponse.ToResponse(serving),
            Quantity = treatmentIngredient.Quantity,
            Calories = serving is null ? 0m : serving.Calories * treatmentIngredient.Quantity,
            Carbs = serving is null ? 0m : serving.Carbs * treatmentIngredient.Quantity,
            Protein = serving is null ? 0m : serving.Protein * treatmentIngredient.Quantity,
            Fat = serving is null ? 0m : serving.Fat * treatmentIngredient.Quantity,
        };
    }
}

public sealed record GetTreatmentServingResponse
{
    public required Guid Id { get; init; }
    public string? Description { get; init; }
    public decimal? ServingAmount { get; init; }
    public string? ServingUnit { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }

    public static GetTreatmentServingResponse ToResponse(IngredientServing serving)
    {
        return new GetTreatmentServingResponse
        {
            Id = serving.Id,
            Description = serving.Description,
            ServingAmount = serving.ServingAmount,
            ServingUnit = serving.ServingUnit,
            Calories = serving.Calories,
            Carbs = serving.Carbs,
            Protein = serving.Protein,
            Fat = serving.Fat,
        };
    }
}