namespace CgmLink.Api.Endpoints.Ingredients;

public sealed record IngredientServingResponse(
    string ServingId,
    string? Description,
    decimal? ServingAmount,
    string? ServingUnit,
    decimal Calories,
    decimal Carbs,
    decimal Protein,
    decimal Fat);
