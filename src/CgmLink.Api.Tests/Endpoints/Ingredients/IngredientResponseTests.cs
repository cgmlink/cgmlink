using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Data.Entities;
using CgmLink.Nutrition.Source;
using NutritionServing = CgmLink.Nutrition.Source.NutritionServing;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.Json;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class IngredientResponseTests
{
    [Test]
    public void LocalMapping_UsesLocalIdsAndOmitsDeletedServings()
    {
        var timestamp = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
        var serving = new IngredientServing
        {
            Created = timestamp, Calories = 10, Carbs = 2, Protein = 3, Fat = 4,
            Description = "one cup", ServingAmount = 100, ServingUnit = "g",
            ExternalId = "legacy-serving",
        };
        var ingredient = new Ingredient
        {
            Name = "Milk", Created = timestamp.AddDays(-1), Updated = timestamp,
            ProductId = "legacy-product", ImageUrl = "image", ThumbnailUrl = "thumbnail",
            Servings = [serving, new IngredientServing
            {
                Created = timestamp, Deleted = timestamp, Calories = 0, Carbs = 0, Protein = 0, Fat = 0,
            }],
        };

        var response = IngredientResponse.FromIngredient(ingredient);

        Assert.Multiple(() =>
        {
            Assert.That(response.IngredientId, Is.EqualTo(ingredient.Id));
            Assert.That(response.ProductId, Is.Null);
            Assert.That(response.Attribution, Is.Null);
            Assert.That(response.DataAsOf, Is.EqualTo(timestamp));
            Assert.That(response.ImageUrl, Is.EqualTo("image"));
            Assert.That(response.ThumbnailUrl, Is.EqualTo("thumbnail"));
            Assert.That(response.Servings.Single(), Is.EqualTo(new IngredientServingResponse(
                serving.Id.ToString(), "one cup", 100, "g", 10, 2, 3, 4)));
        });
    }

    [Test]
    public void ProductMapping_SerializesCommonContractAndPreservesMetadata()
    {
        var timestamp = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
        var attribution = "<a href=\"https://platform.fatsecret.com\">Powered by fatsecret Platform API</a>";
        var product = new NutritionProduct
        {
            ProductId = "123", Name = "Milk", Barcode = "temporary-barcode",
            Servings = [new NutritionServing
            {
                ExternalId = "456", Description = "one cup", ServingAmount = 100, ServingUnit = "g",
                Calories = 10, Carbs = 2, Protein = 3, Fat = 4,
            }],
        };

        var response = IngredientResponse.FromProduct(product, timestamp, attribution);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        Assert.Multiple(() =>
        {
            Assert.That(response.IngredientId, Is.Null);
            Assert.That(response.ProductId, Is.EqualTo("123"));
            Assert.That(response.DataAsOf, Is.EqualTo(timestamp));
            Assert.That(response.Attribution, Is.EqualTo(attribution));
            Assert.That(json.RootElement.GetProperty("attribution").GetString(), Is.EqualTo(attribution));
            Assert.That(response.Servings.Single(), Is.EqualTo(new IngredientServingResponse(
                "456", "one cup", 100, "g", 10, 2, 3, 4)));
            Assert.That(json.RootElement.GetProperty("servings")[0].GetProperty("servingId").GetString(), Is.EqualTo("456"));
            Assert.That(json.RootElement.TryGetProperty("source", out _), Is.False);
            Assert.That(json.RootElement.TryGetProperty("nutritionStatus", out _), Is.False);
            Assert.That(json.RootElement.TryGetProperty("barcode", out _), Is.False);
        });
    }

    [Test]
    public void ProductMapping_RejectsMissingProductIdOrAttribution()
    {
        var attribution = "credit";
        Assert.Multiple(() =>
        {
            Assert.That(() => IngredientResponse.FromProduct(
                new NutritionProduct { ProductId = " ", Name = "Milk" }, DateTimeOffset.UtcNow, attribution), Throws.ArgumentException);
            Assert.That(() => IngredientResponse.FromProduct(
                new NutritionProduct { ProductId = "123", Name = "Milk" }, DateTimeOffset.UtcNow, null), Throws.ArgumentNullException);
            Assert.That(() => IngredientResponse.FromProduct(
                new NutritionProduct { ProductId = "123", Name = "Milk" }, DateTimeOffset.UtcNow, " "), Throws.ArgumentException);
        });
    }
}
