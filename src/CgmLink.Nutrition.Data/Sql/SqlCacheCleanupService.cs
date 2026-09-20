using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.Data.Sql;

public partial class SqlCacheCleanupService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqlCacheCleanupService> _logger;
    private readonly NutritionOptions _options;
    private Timer? _timer;

    private bool _disposed;

    public SqlCacheCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<SqlCacheCleanupService> logger,
        IOptions<NutritionOptions> options)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _timer?.Dispose();
        }

        _disposed = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        StartingCacheCleanupService();

        _timer = new Timer(DoWork, cancellationToken, TimeSpan.FromMinutes(5), _options.CacheCleanupInterval);

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            if (state is not CancellationToken cancellationToken)
            {
                throw new ArgumentException("State must be a CancellationToken", nameof(state));
            }

            await DoWorkAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            CacheCleanupServiceFailed(ex);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        StoppingCacheCleanupService();
        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    internal async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        DeletingExpiredCacheEntries();

        using (var scope = _scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<NutritionCacheDbContext>();
            await db.NutritionProducts.Where(e => e.ExpiresAt < DateTimeOffset.UtcNow).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(LogLevel.Information, "Starting nutrition cache cleanup service.")]
    private partial void StartingCacheCleanupService();

    [LoggerMessage(LogLevel.Information, "Deleting expired nutrition cache entries.")]
    private partial void DeletingExpiredCacheEntries();

    [LoggerMessage(LogLevel.Error, "Nutrition cache cleanup service failed.")]
    private partial void CacheCleanupServiceFailed(Exception ex);

    [LoggerMessage(LogLevel.Information, "Stopping nutrition cache cleanup service.")]
    private partial void StoppingCacheCleanupService();
}