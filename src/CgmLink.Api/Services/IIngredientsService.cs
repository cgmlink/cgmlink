using CgmLink.Data.Entities;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Endpoints.Ingredients.ListIngredients;
using CgmLink.Api.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IIngredientsService
{
    Task<ListIngredientsResponse> ListPersonalIngredientsAsync(
        Guid userId, int page = 0, int pageSize = 20, string? name = null,
        string? sortBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<IngredientResponse>> SearchExternalIngredientsAsync(
        string name, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IngredientResponse> GetIngredientAsync(
        string identifier, Guid userId, CancellationToken cancellationToken = default,
        IngredientType type = IngredientType.Personal);

    Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IMealIngredientRequest> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default);
}
