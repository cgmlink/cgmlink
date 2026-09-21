using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Caching.Ef;

[ExcludeFromCodeCoverage]
internal sealed class NutritionDbInitializer : INutritionDbInitializer
{
    private readonly NutritionCacheDbContext _db;

    public NutritionDbInitializer(NutritionCacheDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task InitialiseCacheAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = (await _db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        if (pendingMigrations.Length != 0)
        {
            await _db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}