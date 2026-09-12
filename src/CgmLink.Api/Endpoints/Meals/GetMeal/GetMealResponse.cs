using CgmLink.Data.Entities;
using System;

namespace CgmLink.Api.Endpoints.Meals.GetMeal;

public sealed record GetMealResponse
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

    public static GetMealResponse ToResponse(Meal meal, int ingredientCount)
    {
        return new GetMealResponse
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
            IngredientCount = ingredientCount,
        };
    }
}