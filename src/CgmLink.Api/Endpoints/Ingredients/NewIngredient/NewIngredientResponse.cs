using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Ingredients.NewIngredient;

public sealed record NewIngredientResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Barcode { get; init; }
    public string? ProductId { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required DateTimeOffset Created { get; init; }
    public ICollection<NewIngredientServingResponse>? Servings { get; init; }

    public sealed record NewIngredientServingResponse
    {
        public required Guid Id { get; init; }
        public string? Description { get; init; }
        public decimal? ServingAmount { get; init; }
        public string? ServingUnit { get; init; }
        public required decimal Calories { get; init; }
        public required decimal Carbs { get; init; }
        public required decimal Protein { get; init; }
        public required decimal Fat { get; init; }
    }
}

internal static class NewIngredientResponseExtensions
{
    internal static NewIngredientResponse ToResponse(this Ingredient ingredient)
    {
        return new NewIngredientResponse
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            Barcode = ingredient.Barcode,
            ProductId = ingredient.ProductId,
            ImageUrl = ingredient.ImageUrl,
            ThumbnailUrl = ingredient.ThumbnailUrl,
            Created = ingredient.Created,
            Servings = ingredient.Servings?.Select(s => new NewIngredientResponse.NewIngredientServingResponse
            {
                Id = s.Id,
                Description = s.Description,
                ServingAmount = s.ServingAmount,
                ServingUnit = s.ServingUnit,
                Calories = s.Calories,
                Carbs = s.Carbs,
                Protein = s.Protein,
                Fat = s.Fat,
            }).ToList(),
        };
    }
}
