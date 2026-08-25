using CgmLink.Nutrition.Data;
using CgmLink.Nutrition.Data.Entities;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace CgmLink.Nutrition.Data.Importer;

public static class ImportRunner
{
    public static async Task RunAsync(CgmLinkNutritionDbContext dbContext, ImporterOptions options)
    {
        var pendingMigrations = (await dbContext.Database.GetPendingMigrationsAsync().ConfigureAwait(false)).ToArray();
        if (pendingMigrations.Length != 0)
        {
            await dbContext.Database.MigrateAsync().ConfigureAwait(false);
        }

        using var reader = File.OpenText(options.Path);

        switch (options.Mode)
        {
            case ImportMode.Rebuild:
                await RebuildAsync(dbContext, reader, options.BatchSize);
                break;
            case ImportMode.Backfill:
                await BackfillAsync(dbContext, reader, options.BatchSize, options.OverwriteMissingImages);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options.Mode), options.Mode, null);
        }
    }

    internal static async Task RebuildAsync(CgmLinkNutritionDbContext dbContext, TextReader reader, int batchSize)
    {
        var products = new List<Product>(batchSize);
        var brands = new List<ProductBrand>(batchSize);
        await foreach (var sourceProduct in ReadProductsAsync(reader))
        {
            if (sourceProduct.Nutriments is null)
            {
                continue;
            }

            var product = MapProduct(sourceProduct);
            products.Add(product);
            brands.AddRange(MapProductBrands(product.Id, sourceProduct.Brands));

            if (products.Count >= batchSize)
            {
                await dbContext.BulkInsertAsync(products);
                await dbContext.BulkInsertAsync(brands);
                products.Clear();
                brands.Clear();
            }
        }

        if (products.Count > 0)
        {
            await dbContext.BulkInsertAsync(products);
            await dbContext.BulkInsertAsync(brands);
        }
    }

    internal static async Task BackfillAsync(
        CgmLinkNutritionDbContext dbContext,
        TextReader reader,
        int batchSize,
        bool overwriteMissingImages)
    {
        var pending = new Dictionary<string, ProductImageData>(StringComparer.Ordinal);

        await foreach (var sourceProduct in ReadProductsAsync(reader))
        {
            if (string.IsNullOrWhiteSpace(sourceProduct.Code))
            {
                continue;
            }

            pending[sourceProduct.Code] = new ProductImageData(sourceProduct.ImageUrl, sourceProduct.ImageThumbUrl);

            if (pending.Count >= batchSize)
            {
                await ApplyBackfillBatchAsync(dbContext, pending, overwriteMissingImages);
                pending.Clear();
            }
        }

        if (pending.Count > 0)
        {
            await ApplyBackfillBatchAsync(dbContext, pending, overwriteMissingImages);
        }
    }

    public static Product MapProduct(OpenFoodFactsProduct sourceProduct)
    {
        return new Product
        {
            Id = Guid.NewGuid().ToString(),
            ProductType = sourceProduct.ProductType,
            Quantity = sourceProduct.Quantity,
            ProductQuantityUnit = sourceProduct.ProductQuantityUnit,
            ProductName = sourceProduct.ProductName,
            ProductQuantity = sourceProduct.ProductQuantity,
            NutritionDataPer = sourceProduct.NutritionDataPer,
            Nutriments = sourceProduct.Nutriments is null
                ? null
                : new Nutriments
                {
                    EnergyUnit = sourceProduct.Nutriments.EnergyUnit,
                    FatUnit = sourceProduct.Nutriments.FatUnit,
                    CarbohydratesUnit = sourceProduct.Nutriments.CarbohydratesUnit,
                    EnergyKcalUnit = sourceProduct.Nutriments.EnergyKcalUnit,
                    EnergyKcalValue = sourceProduct.Nutriments.EnergyKcalValue,
                    EnergyValue = sourceProduct.Nutriments.EnergyValue,
                    CarbohydratesValue = sourceProduct.Nutriments.CarbohydratesValue,
                    Proteins = sourceProduct.Nutriments.Proteins,
                    EnergyKcalValueComputed = sourceProduct.Nutriments.EnergyKcalValueComputed,
                    ProteinsValue = sourceProduct.Nutriments.ProteinsValue,
                    EnergyKcal = sourceProduct.Nutriments.EnergyKcal,
                    ProteinsUnit = sourceProduct.Nutriments.ProteinsUnit,
                    Carbohydrates = sourceProduct.Nutriments.Carbohydrates,
                    Energy = sourceProduct.Nutriments.Energy,
                    Fat = sourceProduct.Nutriments.Fat,
                    FatValue = sourceProduct.Nutriments.FatValue,
                    Energy100g = sourceProduct.Nutriments.Energy100g,
                    EnergyServing = sourceProduct.Nutriments.EnergyServing,
                    EnergyKcal100g = sourceProduct.Nutriments.EnergyKcal100g,
                    EnergyKcalServing = sourceProduct.Nutriments.EnergyKcalServing,
                    Fat100g = sourceProduct.Nutriments.Fat100g,
                    FatServing = sourceProduct.Nutriments.FatServing,
                    Carbohydrates100g = sourceProduct.Nutriments.Carbohydrates100g,
                    CarbohydratesServing = sourceProduct.Nutriments.CarbohydratesServing,
                    Proteins100g = sourceProduct.Nutriments.Proteins100g,
                    ProteinsServing = sourceProduct.Nutriments.ProteinsServing
                },
            NutritionDataPreparedPer = sourceProduct.NutritionDataPreparedPer,
            Code = sourceProduct.Code,
            ServingQuantity = sourceProduct.ServingQuantity,
            ImageUrl = sourceProduct.ImageUrl,
            ImageThumbUrl = sourceProduct.ImageThumbUrl
        };
    }

    public static IEnumerable<ProductBrand> MapProductBrands(string productId, string? rawBrands)
    {
        if (string.IsNullOrWhiteSpace(rawBrands))
        {
            yield break;
        }

        foreach (var name in rawBrands.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            yield return new ProductBrand
            {
                Id = Guid.NewGuid().ToString(),
                ProductId = productId,
                Name = name
            };
        }
    }

    public static void ApplyBackfill(Product target, ProductImageData imageData, bool overwriteMissingImages)
    {
        if (!string.IsNullOrWhiteSpace(imageData.ImageUrl) || overwriteMissingImages)
        {
            target.ImageUrl = imageData.ImageUrl;
        }

        if (!string.IsNullOrWhiteSpace(imageData.ImageThumbUrl) || overwriteMissingImages)
        {
            target.ImageThumbUrl = imageData.ImageThumbUrl;
        }
    }

    internal static async IAsyncEnumerable<OpenFoodFactsProduct> ReadProductsAsync(TextReader reader)
    {
        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var sourceProduct = JsonConvert.DeserializeObject<OpenFoodFactsProduct>(line);
            if (sourceProduct is not null)
            {
                yield return sourceProduct;
            }
        }
    }

    private static async Task ApplyBackfillBatchAsync(
        CgmLinkNutritionDbContext dbContext,
        IReadOnlyDictionary<string, ProductImageData> pending,
        bool overwriteMissingImages)
    {
        var codes = pending.Keys.ToArray();
        var products = await dbContext.Products
            .Where(p => p.Code != null && codes.Contains(p.Code))
            .ToListAsync();

        foreach (var product in products)
        {
            if (product.Code is null)
            {
                continue;
            }

            if (pending.TryGetValue(product.Code, out var imageData))
            {
                ApplyBackfill(product, imageData, overwriteMissingImages);
            }
        }

        if (products.Count > 0)
        {
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
        }
    }
}

public sealed record ProductImageData(string? ImageUrl, string? ImageThumbUrl);
