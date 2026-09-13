using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Meals.ListMealIngredients;

public sealed record ListMealIngredientsResponse
{
    public required ICollection<MealIngredientResponse> Ingredients { get; init; } = [];

    public static ListMealIngredientsResponse ToResponse(Meal meal)
    {
        return new ListMealIngredientsResponse
        {
            Ingredients = meal.Ingredients.Select(MealIngredientResponse.ToResponse).ToList(),
        };
    }
}

public sealed record MealIngredientResponse
{
    public required Guid IngredientId { get; init; }
    public required string IngredientName { get; init; }
    public required decimal Quantity { get; init; }
    public MealServingResponse? Serving { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }

    public static MealIngredientResponse ToResponse(MealIngredient mealIngredient)
    {
        var serving = mealIngredient.Serving;

        return new MealIngredientResponse
        {
            IngredientId = mealIngredient.IngredientId,
            IngredientName = mealIngredient.Ingredient!.Name,
            Quantity = mealIngredient.Quantity,
            Serving = serving is null ? null : MealServingResponse.ToResponse(serving),
            Calories = serving is null ? 0m : serving.Calories * mealIngredient.Quantity,
            Carbs = serving is null ? 0m : serving.Carbs * mealIngredient.Quantity,
            Protein = serving is null ? 0m : serving.Protein * mealIngredient.Quantity,
            Fat = serving is null ? 0m : serving.Fat * mealIngredient.Quantity,
        };
    }
}

public sealed record MealServingResponse
{
    public required Guid Id { get; init; }
    public string? Description { get; init; }

    public static MealServingResponse ToResponse(IngredientServing serving)
    {
        return new MealServingResponse
        {
            Id = serving.Id,
            Description = serving.Description,
        };
    }
}