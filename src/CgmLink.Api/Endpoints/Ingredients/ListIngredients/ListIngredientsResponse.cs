using CgmLink.Api.Models;
using System;
using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

public sealed record ListIngredientsResponse : PagedResponse
{
    public required ICollection<GetIngredientResponse> Ingredients { get; init; } = [];
}

public sealed record GetIngredientResponse
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Barcode { get; init; }
    public string? ProductId { get; init; }
    public string? ImageUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public required DateTimeOffset Created { get; init; }
    public DateTimeOffset? Updated { get; init; }
}