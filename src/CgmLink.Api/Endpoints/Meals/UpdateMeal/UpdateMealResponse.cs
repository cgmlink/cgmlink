using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Meals.UpdateMeal;

public sealed record UpdateMealResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }
    public required DateTimeOffset Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
    public required int IngredientCount { get; init; }
    public ICollection<UpdateMealIngredientResponse>? Ingredients { get; init; }

    public static UpdateMealResponse ToResponse(Meal meal)
    {
        return new UpdateMealResponse
        {
            Id = meal.Id,
            Name = meal.Name,
            ImageUrl = meal.ImageUrl,
            ThumbnailUrl = meal.ThumbnailUrl,
            Calories = meal.Calories,
            Carbs = meal.Carbs,
            Protein = meal.Protein,
            Fat = meal.Fat,
            Created = meal.Created,
            Updated = meal.Updated,
            IngredientCount = meal.Ingredients.Count,
            Ingredients = meal.Ingredients.Select(UpdateMealIngredientResponse.ToResponse).ToList(),
        };
    }
}

public sealed record UpdateMealIngredientResponse
{
    public required Guid IngredientId { get; init; }
    public required string IngredientName { get; init; }
    public required Guid ServingId { get; init; }
    public string? ServingDescription { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }

    public static UpdateMealIngredientResponse ToResponse(MealIngredient mealIngredient)
    {
        var serving = mealIngredient.Serving;
        return new UpdateMealIngredientResponse
        {
            IngredientId = mealIngredient.IngredientId,
            IngredientName = mealIngredient.Ingredient?.Name ?? string.Empty,
            ServingId = serving?.Id ?? mealIngredient.ServingId ?? Guid.Empty,
            ServingDescription = serving?.Description,
            Quantity = mealIngredient.Quantity,
            Calories = serving is null ? 0m : serving.Calories * mealIngredient.Quantity,
            Carbs = serving is null ? 0m : serving.Carbs * mealIngredient.Quantity,
            Protein = serving is null ? 0m : serving.Protein * mealIngredient.Quantity,
            Fat = serving is null ? 0m : serving.Fat * mealIngredient.Quantity,
        };
    }
}