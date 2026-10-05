using CgmLink.Data.Entities;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Endpoints.Ingredients.SearchIngredients;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IIngredientsService
{
    Task<SearchIngredientsResponse> SearchIngredientsAsync(
        string name, Guid userId, int page = 0, int pageSize = 20, bool includeExternal = true,
        CancellationToken cancellationToken = default);

    Task<IngredientResponse> GetIngredientAsync(
        string identifier, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IMealIngredientRequest> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default);
}
