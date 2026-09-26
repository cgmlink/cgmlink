using CgmLink.Nutrition.Source;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Nutrition.Api.Endpoints.GetFood;

public sealed record GetFoodResponse
{
    public required string ProductId { get; init; }
    public required string Name { get; init; }
    public string? Barcode { get; init; }
    public required ICollection<GetFoodServingResponse> Servings { get; init; } = [];

    public static GetFoodResponse ToResponse(NutritionProduct product)
    {
        return new GetFoodResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Barcode = product.Barcode,
            Servings = product.Servings.Select(GetFoodServingResponse.ToResponse).ToList(),
        };
    }
}

public sealed record GetFoodServingResponse
{
    public required string ExternalId { get; init; }
    public string? Description { get; init; }
    public decimal? ServingAmount { get; init; }
    public string? ServingUnit { get; init; }
    public required decimal Calories { get; init; }
    public required decimal Carbs { get; init; }
    public required decimal Protein { get; init; }
    public required decimal Fat { get; init; }

    public static GetFoodServingResponse ToResponse(NutritionServing serving)
    {
        return new GetFoodServingResponse
        {
            ExternalId = serving.ExternalId,
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
