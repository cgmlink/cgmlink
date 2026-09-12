using CgmLink.Data.Entities;
using System;

namespace CgmLink.Api.Endpoints.Meals.NewMeal;

public sealed record NewMealResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }
    public required int IngredientCount { get; init; }
    public required DateTimeOffset Created { get; init; }

    public static NewMealResponse ToResponse(Meal meal)
    {
        return new NewMealResponse
        {
            Id = meal.Id,
            Name = meal.Name,
            ImageUrl = meal.ImageUrl,
            ThumbnailUrl = meal.ThumbnailUrl,
            Calories = meal.Calories,
            Carbs = meal.Carbs,
            Protein = meal.Protein,
            Fat = meal.Fat,
            IngredientCount = meal.Ingredients.Count,
            Created = meal.Created,
        };
    }
}