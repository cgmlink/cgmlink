using CgmLink.Nutrition.Caching;
using CgmLink.Nutrition.Source;
using Microsoft.Extensions.Options;
using CachedProduct = CgmLink.Nutrition.Caching.Entities.NutritionProduct;
using CachedServing = CgmLink.Nutrition.Caching.Entities.NutritionServing;

namespace CgmLink.Nutrition;

internal sealed class NutritionCatalog : INutritionCatalog
{
    private static readonly TimeSpan MaximumCacheLifetime = TimeSpan.FromHours(24);
    private readonly INutritionSourceClient _source;
    private readonly INutritionCache _cache;
    private readonly TimeSpan _cacheLifetime;

    public NutritionCatalog(
        INutritionSourceClient source,
        INutritionCache cache,
        IOptions<NutritionOptions> options)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(options);
        _cacheLifetime = options.Value.CacheExpiry <= MaximumCacheLifetime
            ? options.Value.CacheExpiry
            : MaximumCacheLifetime;
    }

    public Task<IReadOnlyCollection<NutritionProduct>> SearchAsync(
        string searchExpression,
        int pageNumber = 0,
        int maxResults = 20,
        CancellationToken cancellationToken = default) =>
        _source.SearchAsync(searchExpression, pageNumber, maxResults, cancellationToken);

    public async Task<NutritionProduct?> GetAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        var cached = await _cache.GetAsync(_source.Source, productId, cancellationToken).ConfigureAwait(false);
        if (cached?.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Map(cached);
        }

        return await FetchAndCacheAsync(productId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<NutritionProduct?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);
        var cached = await _cache.GetByBarcodeAsync(_source.Source, barcode, cancellationToken).ConfigureAwait(false);
        if (cached?.ExpiresAt > DateTimeOffset.UtcNow)
        {
            return Map(cached, barcode);
        }

        var product = await _source.GetByBarcodeAsync(barcode, cancellationToken).ConfigureAwait(false);
        if (product is not null)
        {
            await CacheAsync(product, barcode, cancellationToken).ConfigureAwait(false);
        }

        return product;
    }

    public Task<NutritionProduct?> RefreshAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        return FetchAndCacheAsync(productId, cancellationToken);
    }

    private async Task<NutritionProduct?> FetchAndCacheAsync(
        string productId,
        CancellationToken cancellationToken)
    {
        var product = await _source.GetAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product is not null)
        {
            await CacheAsync(product, product.Barcode, cancellationToken).ConfigureAwait(false);
        }

        return product;
    }

    private ValueTask CacheAsync(
        NutritionProduct product,
        string? barcode,
        CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.Add(_cacheLifetime);
        return _cache.SetAsync(new CachedProduct
        {
            Source = _source.Source,
            ProductId = product.ProductId,
            Name = product.Name,
            Barcode = barcode,
            ExpiresAt = expiresAt,
            Servings = product.Servings.Select(serving => new CachedServing
            {
                Source = _source.Source,
                ProductId = product.ProductId,
                ServingId = serving.ExternalId,
                Description = serving.Description,
                ServingAmount = serving.ServingAmount,
                ServingUnit = serving.ServingUnit,
                Calories = serving.Calories,
                Carbs = serving.Carbs,
                Protein = serving.Protein,
                Fat = serving.Fat,
            }).ToList(),
        }, cancellationToken);
    }

    private static NutritionProduct Map(CachedProduct product, string? barcode = null) => new()
    {
        ProductId = product.ProductId,
        Name = product.Name,
        Barcode = barcode ?? product.Barcode,
        Servings = product.Servings.Select(serving => new NutritionServing
        {
            ExternalId = serving.ServingId,
            Description = serving.Description,
            ServingAmount = serving.ServingAmount,
            ServingUnit = serving.ServingUnit,
            Calories = serving.Calories,
            Carbs = serving.Carbs,
            Protein = serving.Protein,
            Fat = serving.Fat,
        }).ToArray(),
    };
}
