namespace CgmLink.Api.Endpoints.Ingredients.SearchIngredients;

public sealed record SearchIngredientsResponse(
    IngredientSearchGroup Personal,
    IngredientSearchGroup External);
