using CgmLink.Data.Entities;
using CgmLink.Api.Endpoints.Ingredients;
using System;

namespace CgmLink.Api.Endpoints.Meals.GetMeal;

public sealed record MealNutritionIngredientResponse
{
    public required string ProductId { get; init; }
    public required string IngredientName { get; init; }
    public required decimal Quantity { get; init; }
    public required IngredientServingResponse Serving { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }
    public DateTimeOffset? CachedAt { get; init; }
    public string? Attribution { get; init; }

    public static MealNutritionIngredientResponse ToResponse(
        MealNutritionIngredient mealIngredient, IngredientResponse product, IngredientServingResponse serving)
    {
        return new MealNutritionIngredientResponse
        {
            ProductId = product.ProductId!,
            IngredientName = product.Name,
            Quantity = mealIngredient.Quantity,
            Serving = serving,
            Calories = serving.Calories * mealIngredient.Quantity,
            Carbs = serving.Carbs * mealIngredient.Quantity,
            Protein = serving.Protein * mealIngredient.Quantity,
            Fat = serving.Fat * mealIngredient.Quantity,
            CachedAt = product.CachedAt,
            Attribution = product.Attribution,
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
