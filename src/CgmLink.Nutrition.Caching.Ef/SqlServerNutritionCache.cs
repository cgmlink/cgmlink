using CgmLink.Nutrition.Caching.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Caching.Ef;

internal sealed class SqlServerNutritionCache : INutritionCache
{
    private readonly NutritionCacheDbContext _db;

    public SqlServerNutritionCache(NutritionCacheDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async ValueTask<NutritionProduct?> GetAsync(string source, string productId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        if (string.IsNullOrWhiteSpace(productId))
        {
            throw new ArgumentException("Product id must not be empty.", nameof(productId));
        }

        var product = await _db.NutritionProducts
            .Include(p => p.Servings)
            .FirstOrDefaultAsync(p => p.Source == source && p.ProductId == productId, cancellationToken)
            .ConfigureAwait(false);

        return product?.ExpiresAt > DateTimeOffset.UtcNow ? product : null;
    }

    public async ValueTask<NutritionProduct?> GetByBarcodeAsync(
        string source,
        string barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        var product = await _db.NutritionProducts
            .Include(p => p.Servings)
            .FirstOrDefaultAsync(p => p.Source == source && p.Barcode == barcode, cancellationToken)
            .ConfigureAwait(false);

        return product?.ExpiresAt > DateTimeOffset.UtcNow ? product : null;
    }

    public async ValueTask SetAsync(NutritionProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (!string.IsNullOrWhiteSpace(product.Barcode))
        {
            var previousMatches = await _db.NutritionProducts
                .Where(p => p.Source == product.Source
                    && p.Barcode == product.Barcode
                    && p.ProductId != product.ProductId)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var previousMatch in previousMatches)
            {
                previousMatch.Barcode = null;
            }
        }

        var existing = await _db.NutritionProducts
            .Include(p => p.Servings)
            .FirstOrDefaultAsync(p => p.Source == product.Source && p.ProductId == product.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _db.NutritionProducts.Add(product);
        }
        else
        {
            existing.Name = product.Name;
            existing.Barcode = product.Barcode;
            existing.ExpiresAt = product.ExpiresAt;
            existing.CachedAt = product.CachedAt;

            var servings = product.Servings.ToDictionary(serving => serving.ServingId);
            _db.NutritionServings.RemoveRange(existing.Servings.Where(serving => !servings.ContainsKey(serving.ServingId)));
            foreach (var serving in product.Servings)
            {
                var cachedServing = existing.Servings.FirstOrDefault(item => item.ServingId == serving.ServingId);
                if (cachedServing is null)
                {
                    existing.Servings.Add(serving);
                    continue;
                }

                cachedServing.Description = serving.Description;
                cachedServing.ServingAmount = serving.ServingAmount;
                cachedServing.ServingUnit = serving.ServingUnit;
                cachedServing.Calories = serving.Calories;
                cachedServing.Carbs = serving.Carbs;
                cachedServing.Protein = serving.Protein;
                cachedServing.Fat = serving.Fat;
            }
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
