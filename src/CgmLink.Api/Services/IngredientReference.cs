using System;

namespace CgmLink.Api.Services;

public sealed record IngredientReference(
    Guid? IngredientId,
    string? ProductId,
    string ServingId,
    decimal Quantity)
{
    public static IngredientReference FromPersonal(IMealIngredientRequest ingredient) =>
        new(ingredient.IngredientId, null, ingredient.ServingId.ToString(), ingredient.Quantity);

    public static IngredientReference FromNutrition(IMealNutritionIngredientRequest ingredient) =>
        new(null, ingredient.ProductId, ingredient.ServingId, ingredient.Quantity);
}
