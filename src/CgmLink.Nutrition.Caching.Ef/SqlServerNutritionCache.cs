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

        return await _db.NutritionProducts
            .FirstOrDefaultAsync(p => p.Source == source && p.ProductId == productId && p.ExpiresAt > DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);
    }

    public async ValueTask SetAsync(NutritionProduct product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existing = await _db.NutritionProducts
            .FirstOrDefaultAsync(p => p.Source == product.Source && p.ProductId == product.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            _db.NutritionProducts.Add(product);
        }
        else
        {
            existing.Barcode = product.Barcode;
            existing.Name = product.Name;
            existing.Calories = product.Calories;
            existing.Carbs = product.Carbs;
            existing.Protein = product.Protein;
            existing.Fat = product.Fat;
            existing.ExpiresAt = product.ExpiresAt;
        }

        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}