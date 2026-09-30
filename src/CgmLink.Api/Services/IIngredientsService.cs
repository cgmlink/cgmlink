using CgmLink.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public interface IIngredientsService
{
    Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IMealIngredientRequest> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default);
}
