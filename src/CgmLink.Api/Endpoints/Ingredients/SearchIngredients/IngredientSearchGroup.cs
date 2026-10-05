using System.Collections.Generic;

namespace CgmLink.Api.Endpoints.Ingredients.SearchIngredients;

public sealed record IngredientSearchGroup(
    IReadOnlyCollection<IngredientResponse> Ingredients,
    int Page,
    int PageSize,
    int? NumberOfPages,
    bool Available = true);
