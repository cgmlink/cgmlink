using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Endpoints.Ingredients;
using System.Net;
using System.Net.Http;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Nutrition;
using CatalogServing = CgmLink.Nutrition.Source.NutritionServing;
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
    private readonly INutritionCatalog _nutritionCatalog;
    private readonly IRepository<NutritionIngredient> _nutritionIngredientsRepository;

    public IngredientsService(
        IRepository<Ingredient> ingredientsRepository,
        INutritionCatalog nutritionCatalog,
        IRepository<NutritionIngredient> nutritionIngredientsRepository)
    {
        _ingredientsRepository = ingredientsRepository ?? throw new ArgumentNullException(nameof(ingredientsRepository));
        _nutritionCatalog = nutritionCatalog ?? throw new ArgumentNullException(nameof(nutritionCatalog));
        _nutritionIngredientsRepository = nutritionIngredientsRepository ??
            throw new ArgumentNullException(nameof(nutritionIngredientsRepository));
    }

    public async Task<IngredientResponse> GetIngredientAsync(
        string identifier, Guid userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (Guid.TryParse(identifier, out var ingredientId))
        {
            var ingredient = await _ingredientsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
                .Include(item => item.Servings.Where(serving => serving.Deleted == null))
                .FirstOrDefaultAsync(item => item.Id == ingredientId && item.UserId == userId && item.Deleted == null,
                    cancellationToken).ConfigureAwait(false);
            if (ingredient is not null)
            {
                return IngredientResponse.FromIngredient(ingredient);
            }
        }

        Nutrition.Source.NutritionProduct? product;
        try
        {
            product = await _nutritionCatalog.GetAsync(identifier, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is HttpRequestException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new ApiException("NUTRITION_UNAVAILABLE", null, HttpStatusCode.ServiceUnavailable);
        }
        if (product is null)
        {
            throw new NotFoundException("INGREDIENT_NOT_FOUND");
        }
        return IngredientResponse.FromProduct(product, product.DataAsOf, product.Attribution);
    }

    public async Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var selections = references.Select((reference, index) =>
        {
            if (reference.IngredientId.HasValue == !string.IsNullOrWhiteSpace(reference.ProductId))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            if (reference.IngredientId is Guid ingredientId)
            {
                if (!Guid.TryParse(reference.ServingId, out var servingId))
                {
                    throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
                }

                return new Selection(index, reference, ingredientId, servingId);
            }

            if (string.IsNullOrWhiteSpace(reference.ServingId))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            return new Selection(index, reference, null, null);
        }).ToList();

        var localSelections = selections.Where(selection => selection.IngredientId.HasValue).ToList();
        var ingredientLookup = localSelections.Count == 0
            ? []
            : await GetIngredientLookupAsync(
                    localSelections.Select(selection => (selection.IngredientId!.Value, selection.LocalServingId!.Value)).ToList(),
                    userId,
                    cancellationToken)
                .ConfigureAwait(false);

        var externalSelections = selections.Where(selection => !selection.IngredientId.HasValue).ToList();
        var externalProducts = new Dictionary<string, CgmLink.Nutrition.Source.NutritionProduct>();
        foreach (var productId in externalSelections.Select(selection => selection.Reference.ProductId!).Distinct())
        {
            var product = await _nutritionCatalog.GetAsync(productId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            externalProducts.Add(productId, product);
        }

        var externalServings = new Dictionary<int, CatalogServing>();
        foreach (var selection in externalSelections)
        {
            var serving = externalProducts[selection.Reference.ProductId!].Servings
                .SingleOrDefault(candidate => candidate.ExternalId == selection.Reference.ServingId);
            if (serving is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            externalServings.Add(selection.Index, serving);
        }

        var identityLookup = await GetOrCreateExternalIdentitiesAsync(
                externalSelections,
                cancellationToken)
            .ConfigureAwait(false);

        var resolved = new ResolvedIngredient[selections.Count];
        foreach (var selection in localSelections)
        {
            var serving = ingredientLookup[selection.IngredientId!.Value].Servings
                .Single(candidate => candidate.Id == selection.LocalServingId);

            resolved[selection.Index] = new ResolvedIngredient(
                selection.IngredientId.Value,
                selection.LocalServingId!.Value,
                selection.Reference.Quantity,
                serving.Calories,
                serving.Carbs,
                serving.Protein,
                serving.Fat);
        }

        foreach (var selection in externalSelections)
        {
            var identity = identityLookup[selection.Reference.ProductId!];
            var identityServing = identity.Servings.Single(serving => serving.ServingId == selection.Reference.ServingId);
            var serving = externalServings[selection.Index];
            resolved[selection.Index] = new ResolvedIngredient(
                identity.Id,
                identityServing.Id,
                selection.Reference.Quantity,
                serving.Calories,
                serving.Carbs,
                serving.Protein,
                serving.Fat);
        }

        return resolved;
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

    private async Task<Dictionary<string, NutritionIngredient>> GetOrCreateExternalIdentitiesAsync(
        IReadOnlyCollection<Selection> selections,
        CancellationToken cancellationToken)
    {
        if (selections.Count == 0)
        {
            return [];
        }

        var productIds = selections.Select(selection => selection.Reference.ProductId!).Distinct().ToList();
        var identities = await _nutritionIngredientsRepository.GetAll()
            .Where(identity => identity.Source == _nutritionCatalog.Source && productIds.Contains(identity.ProductId))
            .Include(identity => identity.Servings)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var identityLookup = identities.ToDictionary(identity => identity.ProductId);
        var newIdentities = new List<NutritionIngredient>();
        var updatedIdentities = new List<NutritionIngredient>();

        foreach (var productId in productIds)
        {
            if (!identityLookup.TryGetValue(productId, out var identity))
            {
                identity = new NutritionIngredient { Source = _nutritionCatalog.Source, ProductId = productId };
                identityLookup.Add(productId, identity);
                newIdentities.Add(identity);
            }

            var servingIds = selections
                .Where(selection => selection.Reference.ProductId == productId)
                .Select(selection => selection.Reference.ServingId)
                .Distinct();
            var changed = false;
            foreach (var servingId in servingIds.Where(servingId => identity.Servings.All(serving => serving.ServingId != servingId)))
            {
                identity.Servings.Add(new NutritionServing
                {
                    NutritionIngredient = identity,
                    NutritionIngredientId = identity.Id,
                    ServingId = servingId,
                });
                changed = true;
            }

            if (changed && !newIdentities.Contains(identity))
            {
                updatedIdentities.Add(identity);
            }
        }

        if (newIdentities.Count > 0)
        {
            await _nutritionIngredientsRepository.AddManyAsync(newIdentities, cancellationToken).ConfigureAwait(false);
        }

        foreach (var identity in updatedIdentities)
        {
            await _nutritionIngredientsRepository.UpdateAsync(identity, cancellationToken).ConfigureAwait(false);
        }

        return identityLookup;
    }

    private sealed record Selection(
        int Index,
        IngredientReference Reference,
        Guid? IngredientId,
        Guid? LocalServingId);
}
