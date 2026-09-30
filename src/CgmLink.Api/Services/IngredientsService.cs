using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Resources;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Services;

public sealed class IngredientsService : IIngredientsService
{
    private readonly IRepository<Ingredient> _ingredientsRepository;

    public IngredientsService(IRepository<Ingredient> ingredientsRepository)
    {
        _ingredientsRepository = ingredientsRepository;
    }

    public async Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var ingredients = references.Select(reference =>
        {
            if (reference.IngredientId is not Guid ingredientId ||
                reference.ProductId is not null ||
                !Guid.TryParse(reference.ServingId, out var servingId))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            return (IngredientId: ingredientId, ServingId: servingId, reference.Quantity);
        }).ToList();

        var ingredientLookup = await GetIngredientLookupAsync(
                ingredients.Select(ingredient => (ingredient.IngredientId, ingredient.ServingId)).ToList(),
                userId,
                cancellationToken)
            .ConfigureAwait(false);

        return ingredients.Select(reference =>
        {
            var serving = ingredientLookup[reference.IngredientId].Servings
                .Single(candidate => candidate.Id == reference.ServingId);

            return new ResolvedIngredient(
                reference.IngredientId,
                reference.ServingId,
                reference.Quantity,
                serving.Calories,
                serving.Carbs,
                serving.Protein,
                serving.Fat);
        }).ToList();
    }

    public async Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IMealIngredientRequest> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var references = ingredients.Select(ingredient => (ingredient.IngredientId, ingredient.ServingId)).ToList();

        return await GetIngredientLookupAsync(references, userId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Dictionary<Guid, Ingredient>> GetIngredientLookupAsync(
        IReadOnlyCollection<(Guid IngredientId, Guid ServingId)> references,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var ingredientIds = references.Select(reference => reference.IngredientId).Distinct().ToList();
        var servingIds = references.Select(reference => reference.ServingId).Distinct().ToList();

        var ingredientLookup = await _ingredientsRepository.GetAll()
            .Where(i => ingredientIds.Contains(i.Id) && i.UserId == userId && i.Deleted == null)
            .Include(i => i.Servings.Where(s => servingIds.Contains(s.Id) && s.Deleted == null))
            .ToDictionaryAsync(i => i.Id, cancellationToken)
            .ConfigureAwait(false);

        foreach (var reference in references)
        {
            if (!ingredientLookup.TryGetValue(reference.IngredientId, out var ingredient) ||
                !ingredient.Servings.Any(serving => serving.Id == reference.ServingId))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
        }

        return ingredientLookup;
    }
}
