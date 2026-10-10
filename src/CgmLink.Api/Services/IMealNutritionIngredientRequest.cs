namespace CgmLink.Api.Services;

public interface IMealNutritionIngredientRequest
{
    string ProductId { get; }
    string ServingId { get; }
    decimal Quantity { get; }
}
