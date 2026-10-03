using CgmLink.Nutrition.Caching;
using CgmLink.Nutrition.Source;
using Microsoft.Extensions.Options;
using Moq;
using CachedProduct = CgmLink.Nutrition.Caching.Entities.NutritionProduct;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class NutritionCatalogTests
{
    private Mock<INutritionSourceClient> _source = null!;
    private Mock<INutritionCache> _cache = null!;
    private NutritionCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _source = new Mock<INutritionSourceClient>();
        _source.SetupGet(source => source.Source).Returns("test");
        _source.SetupGet(source => source.Attribution).Returns("Current provider attribution");
        _cache = new Mock<INutritionCache>();
        _catalog = new NutritionCatalog(
            _source.Object,
            _cache.Object,
            Options.Create(new NutritionOptions { CacheExpiry = TimeSpan.FromHours(48) }));
    }

    [Test]
    public async Task GetAsync_Returns_Unexpired_Cached_Product_With_All_Servings()
    {
        var timestamp = DateTimeOffset.UtcNow.AddHours(-1);
        var cached = CachedProduct(expiresAt: DateTimeOffset.UtcNow.AddMinutes(1));
        cached.DataAsOf = timestamp;
        _cache.Setup(cache => cache.GetAsync("test", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        var product = await _catalog.GetAsync("1");

        Assert.That(product!.Servings.Select(serving => serving.ExternalId), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(product.DataAsOf, Is.EqualTo(timestamp));
        Assert.That(product.Attribution, Is.EqualTo("Current provider attribution"));
        _source.Verify(source => source.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetAsync_Refreshes_Expired_Cached_Product_And_Caps_Expiry_At_24_Hours()
    {
        _cache.Setup(cache => cache.GetAsync("test", "1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CachedProduct(expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1)));
        _source.Setup(source => source.GetAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SourceProduct("fresh"));
        CachedProduct stored = null!;
        _cache.Setup(cache => cache.SetAsync(It.IsAny<CachedProduct>(), It.IsAny<CancellationToken>()))
            .Callback<CachedProduct, CancellationToken>((product, _) => stored = product);

        var before = DateTimeOffset.UtcNow;
        var product = await _catalog.GetAsync("1");

        Assert.Multiple(() =>
        {
            Assert.That(product!.Name, Is.EqualTo("fresh"));
            Assert.That(stored!.ExpiresAt, Is.InRange(before.AddHours(24), DateTimeOffset.UtcNow.AddHours(24)));
            Assert.That(product.DataAsOf, Is.InRange(before, DateTimeOffset.UtcNow));
            Assert.That(stored.DataAsOf, Is.EqualTo(product.DataAsOf));
        });
    }

    [Test]
    public async Task GetByBarcodeAsync_Uses_Cache_Before_Provider()
    {
        _cache.Setup(cache => cache.GetByBarcodeAsync("test", "123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CachedProduct(expiresAt: DateTimeOffset.UtcNow.AddMinutes(1)));

        var product = await _catalog.GetByBarcodeAsync("123");

        Assert.That(product!.Barcode, Is.EqualTo("123"));
        _source.Verify(source => source.GetByBarcodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CachedProduct CachedProduct(DateTimeOffset expiresAt) => new()
    {
        Source = "test",
        ProductId = "1",
        Name = "cached",
        Barcode = "123",
        ExpiresAt = expiresAt,
        Servings =
        [
            new() { Source = "test", ProductId = "1", ServingId = "a" },
            new() { Source = "test", ProductId = "1", ServingId = "b" },
        ],
    };

    private static NutritionProduct SourceProduct(string name) => new()
    {
        ProductId = "1",
        Name = name,
        Servings =
        [
            new NutritionServing { ExternalId = "a" },
            new NutritionServing { ExternalId = "b" },
        ],
    };
}
