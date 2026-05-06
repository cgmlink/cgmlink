using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CgmLink.Data;

[ExcludeFromCodeCoverage]
public sealed class CgmLinkDbInitializer
{
    private readonly CgmLinkDbContext _db;

    public CgmLinkDbInitializer(CgmLinkDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task InitialiseDbAsync(CancellationToken cancellationToken)
    {
        var pendingMigrations = (await _db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToArray();
        if (pendingMigrations.Length != 0)
        {
            await _db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}