using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Ingredients.ListIngredients;

public sealed record ListIngredientsResponse
{
    public required IReadOnlyCollection<IngredientResponse> Ingredients { get; init; }
    public required int? NumberOfPages { get; init; }
}
