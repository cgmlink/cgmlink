using CgmLink.Nutrition.Caching.Ef;
using CgmLink.Nutrition.Caching.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class SqlServerNutritionCacheTests
{
    [Test]
    public async Task SetAsync_RoundTrips_Servings_And_Clears_Barcode_On_Product_Refresh()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NutritionCacheDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new NutritionCacheDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var cache = new SqlServerNutritionCache(db);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var retrievedAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        await cache.SetAsync(new NutritionProduct
        {
            Source = "test",
            ProductId = "1",
            Name = "Product",
            CachedAt = retrievedAt,
            Barcode = "123",
            ExpiresAt = expiresAt,
            Servings =
            [
                new() { Source = "test", ProductId = "1", ServingId = "a" },
                new() { Source = "test", ProductId = "1", ServingId = "b" },
            ],
        });
        db.ChangeTracker.Clear();

        var initialByBarcode = await cache.GetByBarcodeAsync("test", "123");
        Assert.That(initialByBarcode!.ProductId, Is.EqualTo("1"));
        Assert.That(initialByBarcode.CachedAt, Is.EqualTo(retrievedAt));
        db.ChangeTracker.Clear();

        await cache.SetAsync(new NutritionProduct
        {
            Source = "test",
            ProductId = "1",
            Name = "Refreshed Product",
            CachedAt = retrievedAt.AddMinutes(5),
            ExpiresAt = expiresAt,
            Servings =
            [
                new() { Source = "test", ProductId = "1", ServingId = "a", Calories = 42 },
                new() { Source = "test", ProductId = "1", ServingId = "c" },
            ],
        });
        db.ChangeTracker.Clear();

        var byProduct = await cache.GetAsync("test", "1");
        db.ChangeTracker.Clear();
        var byBarcode = await cache.GetByBarcodeAsync("test", "123");

        Assert.Multiple(() =>
        {
            Assert.That(byProduct!.Servings.Select(serving => serving.ServingId), Is.EquivalentTo(new[] { "a", "c" }));
            Assert.That(byProduct.Servings.Single(serving => serving.ServingId == "a").Calories, Is.EqualTo(42));
            Assert.That(byBarcode, Is.Null);
            Assert.That(byProduct.CachedAt, Is.EqualTo(retrievedAt.AddMinutes(5)));
        });
    }

    [Test]
    public async Task GetAsync_Does_Not_Return_Expired_Product()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NutritionCacheDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new NutritionCacheDbContext(options);
        await db.Database.EnsureCreatedAsync();
        db.NutritionProducts.Add(new NutritionProduct
        {
            Source = "test",
            ProductId = "1",
            Name = "Expired",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var product = await new SqlServerNutritionCache(db).GetAsync("test", "1");

        Assert.That(product, Is.Null);
    }

    [Test]
    public async Task SetAsync_Moves_Barcode_To_The_Latest_Product()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<NutritionCacheDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new NutritionCacheDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var cache = new SqlServerNutritionCache(db);
        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        await cache.SetAsync(new NutritionProduct
        {
            Source = "test",
            ProductId = "1",
            Name = "Old",
            Barcode = "123",
            ExpiresAt = expiresAt,
        });
        await cache.SetAsync(new NutritionProduct
        {
            Source = "test",
            ProductId = "2",
            Name = "New",
            Barcode = "123",
            ExpiresAt = expiresAt,
        });
        db.ChangeTracker.Clear();

        var product = await cache.GetByBarcodeAsync("test", "123");

        Assert.That(product!.ProductId, Is.EqualTo("2"));
    }
}
