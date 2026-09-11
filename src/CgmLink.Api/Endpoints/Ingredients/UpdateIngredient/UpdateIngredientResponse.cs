using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;

public sealed record UpdateIngredientResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Barcode { get; init; }
    public string? ProductId { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required DateTimeOffset Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
    public ICollection<UpdateIngredientServingResponse>? Servings { get; init; }

    public sealed record UpdateIngredientServingResponse
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

    public static UpdateIngredientResponse ToResponse(Ingredient ingredient)
    {
        return new UpdateIngredientResponse
        {
            Id = ingredient.Id,
            Name = ingredient.Name,
            Barcode = ingredient.Barcode,
            ProductId = ingredient.ProductId,
            ImageUrl = ingredient.ImageUrl,
            ThumbnailUrl = ingredient.ThumbnailUrl,
            Created = ingredient.Created,
            Updated = ingredient.Updated,
            Servings = ingredient.Servings?.Select(s => new UpdateIngredientServingResponse
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