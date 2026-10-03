using CgmLink.Data.Entities;
using CgmLink.Nutrition.Source;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Ingredients;

public sealed record IngredientResponse
{
    public Guid? IngredientId { get; private init; }
    public string? ProductId { get; private init; }
    public string Name { get; private init; } = string.Empty;
    public string? ImageUrl { get; private init; }
    public string? ThumbnailUrl { get; private init; }
    public DateTimeOffset? DataAsOf { get; private init; }
    public string? Attribution { get; private init; }
    public IReadOnlyCollection<IngredientServingResponse> Servings { get; private init; } = [];

    private IngredientResponse() { }

    public static IngredientResponse FromIngredient(Ingredient ingredient) => new()
    {
        IngredientId = ingredient.Id,
        Name = ingredient.Name,
        ImageUrl = ingredient.ImageUrl,
        ThumbnailUrl = ingredient.ThumbnailUrl,
        DataAsOf = ingredient.Updated ?? ingredient.Created,
        Servings = ingredient.Servings.Where(serving => serving.Deleted == null)
            .Select(serving => new IngredientServingResponse(
                serving.Id.ToString(), serving.Description, serving.ServingAmount, serving.ServingUnit,
                serving.Calories, serving.Carbs, serving.Protein, serving.Fat)).ToArray(),
    };

    public static IngredientResponse FromProduct(
        NutritionProduct product, DateTimeOffset? dataAsOf, string attribution)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(product.ProductId);
        ArgumentException.ThrowIfNullOrWhiteSpace(attribution);
        return new IngredientResponse
        {
            ProductId = product.ProductId,
            Name = product.Name,
            DataAsOf = dataAsOf,
            Attribution = attribution,
            Servings = product.Servings.Select(serving => new IngredientServingResponse(
                serving.ExternalId, serving.Description, serving.ServingAmount, serving.ServingUnit,
                serving.Calories, serving.Carbs, serving.Protein, serving.Fat)).ToArray(),
        };
    }
}


