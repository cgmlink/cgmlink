using CgmLink.AspNetCore.Exceptions;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Endpoints.Ingredients.ListIngredients;
using CgmLink.Api.Models;
using CgmLink.Data.Extensions;
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

    public async Task<ListIngredientsResponse> ListPersonalIngredientsAsync(
        Guid userId, int page = 0, int pageSize = 20, string? name = null,
        string? sortBy = null, SortDirection? sortDirection = null, CancellationToken cancellationToken = default)
    {
        name = name?.Trim();
        sortBy = string.IsNullOrWhiteSpace(sortBy) ? nameof(Ingredient.Created) : sortBy;
        var descending = (sortDirection ?? SortDirection.Desc) == SortDirection.Desc;

        var ingredients = await _ingredientsRepository.Find(i => i.UserId == userId && i.Deleted == null &&
                (name == null || i.Name.Contains(name)), new FindOptions { IsAsNoTracking = true })
            .OrderByProperty(sortBy, descending)
            .Skip(page * pageSize)
            .Take(pageSize)
            .Include(i => i.Servings.Where(s => s.Deleted == null))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var totalIngredients = await _ingredientsRepository.CountAsync(i => i.UserId == userId && i.Deleted == null &&
            (name == null || i.Name.Contains(name)), cancellationToken).ConfigureAwait(false);

        return new ListIngredientsResponse
        {
            Ingredients = ingredients.Select(IngredientResponse.FromIngredient).ToArray(),
            NumberOfPages = (int)Math.Ceiling(totalIngredients / (double)pageSize),
        };
    }

    public async Task<IReadOnlyCollection<IngredientResponse>> SearchExternalIngredientsAsync(
        string name, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegative(page);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        try
        {
            var products = await _nutritionCatalog.SearchAsync(name.Trim(), page, pageSize, cancellationToken).ConfigureAwait(false);
            return products.Select(product => IngredientResponse.FromProduct(product, null, product.Attribution)).ToArray();
        }
        catch (Exception exception) when (exception is HttpRequestException ||
            exception is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new ApiException("NUTRITION_UNAVAILABLE", null, HttpStatusCode.ServiceUnavailable);
        }
    }

    public async Task<IngredientResponse> GetIngredientAsync(
        string identifier, Guid userId, CancellationToken cancellationToken = default,
        IngredientType type = IngredientType.Personal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        if (!Enum.IsDefined(type))
        {
            throw new BadRequestException("INGREDIENT_TYPE_INVALID");
        }
        if (type == IngredientType.Personal)
        {
            if (!Guid.TryParse(identifier, out var ingredientId))
            {
                throw new NotFoundException(ValidationMessages.IngredientNotFound);
            }
            var ingredient = await _ingredientsRepository.GetAll(new FindOptions { IsAsNoTracking = true })
                .Include(item => item.Servings.Where(serving => serving.Deleted == null))
                .FirstOrDefaultAsync(item => item.Id == ingredientId && item.UserId == userId && item.Deleted == null,
                    cancellationToken).ConfigureAwait(false);
            if (ingredient is not null)
            {
                return IngredientResponse.FromIngredient(ingredient);
            }
            throw new NotFoundException(ValidationMessages.IngredientNotFound);
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
            throw new NotFoundException(ValidationMessages.IngredientNotFound);
        }
        return IngredientResponse.FromProduct(product, product.CachedAt, product.Attribution);
    }

    public async Task<Dictionary<Guid, (IngredientResponse Product, IngredientServingResponse Serving)>> GetNutritionServingsAsync(
        IEnumerable<(NutritionIngredient? Ingredient, CgmLink.Data.Entities.NutritionServing? Serving)> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var products = new Dictionary<string, IngredientResponse>();
        var servings = new Dictionary<Guid, (IngredientResponse Product, IngredientServingResponse Serving)>();
        foreach (var (identity, servingIdentity) in ingredients)
        {
            if (identity is null || identity.Source != _nutritionCatalog.Source)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
            if (servingIdentity is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            }
            if (!products.TryGetValue(identity.ProductId, out var product))
            {
                product = await GetIngredientAsync(
                    identity.ProductId, userId, cancellationToken, IngredientType.External).ConfigureAwait(false);
                products.Add(identity.ProductId, product);
            }
            var serving = product.Servings.SingleOrDefault(s => s.ServingId == servingIdentity.ServingId)
                ?? throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            servings[servingIdentity.Id] = (product, serving);
        }
        return servings;
    }

    public Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        CancellationToken cancellationToken = default)
        => ResolveIngredientsAsync(references, userId, null, cancellationToken);

    public Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<(NutritionIngredient? Ingredient, CgmLink.Data.Entities.NutritionServing? Serving, decimal Quantity)> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var references = new List<IngredientReference>();
        var storedIds = new List<(Guid IngredientId, Guid ServingId)>();
        foreach (var (ingredient, serving, quantity) in ingredients)
        {
            if (ingredient is null || ingredient.Source != _nutritionCatalog.Source)
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
            if (serving is null)
            {
                throw new BadRequestException(ValidationMessages.IngredientServingIdInvalid);
            }
            references.Add(new IngredientReference(null, ingredient.ProductId, serving.ServingId, quantity));
            storedIds.Add((ingredient.Id, serving.Id));
        }

        return ResolveIngredientsAsync(references, userId, storedIds, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<(Guid IngredientId, Guid ServingId, decimal Quantity)> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var selections = ingredients.ToList();
        var ids = selections.Select(i => i.IngredientId).Distinct().ToList();
        var identities = await _nutritionIngredientsRepository.GetAll()
            .Where(i => ids.Contains(i.Id))
            .Include(i => i.Servings)
            .ToDictionaryAsync(i => i.Id, cancellationToken).ConfigureAwait(false);
        var references = selections.Select(i =>
        {
            if (!identities.TryGetValue(i.IngredientId, out var ingredient))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
            return ((NutritionIngredient?)ingredient, ingredient.Servings.SingleOrDefault(s => s.Id == i.ServingId), i.Quantity);
        }).ToList();
        return await ResolveIngredientsAsync(references, userId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IReadOnlyCollection<ResolvedIngredient>> ResolveIngredientsAsync(
        IEnumerable<IngredientReference> references,
        Guid userId,
        IReadOnlyList<(Guid IngredientId, Guid ServingId)>? storedIds,
        CancellationToken cancellationToken)
    {
        var selections = references.Select(CreateSelection).ToList();

        var localSelections = selections.Where(selection => selection.IngredientId.HasValue).ToList();
        var ingredientLookup = localSelections.Count == 0
            ? []
            : await GetIngredientLookupAsync(
                    localSelections.Select(selection => (selection.IngredientId!.Value, selection.LocalServingId!.Value)).ToList(),
                    userId,
                    cancellationToken)
                .ConfigureAwait(false);

        var externalSelections = selections.Where(selection => !selection.IngredientId.HasValue).ToList();
        var externalServings = await GetExternalServingsAsync(
            externalSelections, storedIds is not null, cancellationToken).ConfigureAwait(false);

        var identityLookup = storedIds is not null ? [] : await GetOrCreateExternalIdentitiesAsync(
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
            var identity = storedIds is null ? identityLookup[selection.Reference.ProductId!] : null;
            var identityServing = identity?.Servings.Single(serving => serving.ServingId == selection.Reference.ServingId);
            var serving = externalServings[selection.Index];
            resolved[selection.Index] = new ResolvedIngredient(
                storedIds?[selection.Index].IngredientId ?? identity!.Id,
                storedIds?[selection.Index].ServingId ?? identityServing!.Id,
                selection.Reference.Quantity,
                serving.Calories,
                serving.Carbs,
                serving.Protein,
                serving.Fat);
        }

        return resolved;
    }

    private static Selection CreateSelection(IngredientReference reference, int index)
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
    }

    private async Task<Dictionary<int, CatalogServing>> GetExternalServingsAsync(
        IReadOnlyCollection<Selection> selections,
        bool useStoredIdentities,
        CancellationToken cancellationToken)
    {
        var products = new Dictionary<string, CgmLink.Nutrition.Source.NutritionProduct>();
        foreach (var productId in selections.Select(selection => selection.Reference.ProductId!).Distinct())
        {
            var product = await _nutritionCatalog.GetAsync(productId, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                if (useStoredIdentities)
                {
                    throw new NotFoundException(ValidationMessages.IngredientNotFound);
                }
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }

            products.Add(productId, product);
        }

        var servings = new Dictionary<int, CatalogServing>();
        foreach (var selection in selections)
        {
            var serving = products[selection.Reference.ProductId!].Servings
                .SingleOrDefault(candidate => candidate.ExternalId == selection.Reference.ServingId);
            if (serving is null)
            {
                throw new BadRequestException(useStoredIdentities
                    ? ValidationMessages.IngredientServingIdInvalid
                    : ValidationMessages.IngredientIdInvalid);
            }

            servings.Add(selection.Index, serving);
        }

        return servings;
    }

    public Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IMealIngredientRequest> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
        => GetValidatedIngredientsAsync(ingredients.Select(IngredientReference.FromPersonal), userId, cancellationToken);

    public async Task<Dictionary<Guid, Ingredient>> GetValidatedIngredientsAsync(
        IEnumerable<IngredientReference> ingredients,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var references = ingredients.Select(ingredient =>
        {
            if (ingredient.IngredientId is not Guid id || !string.IsNullOrWhiteSpace(ingredient.ProductId) ||
                !Guid.TryParse(ingredient.ServingId, out var servingId))
            {
                throw new BadRequestException(ValidationMessages.IngredientIdInvalid);
            }
            return (id, servingId);
        }).ToList();

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
