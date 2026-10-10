using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Data.Entities;
using CgmLink.Nutrition.Source;
using NUnit.Framework;
using System;
using System.Linq;

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
            Created = timestamp,
            Calories = 10,
            Carbs = 2,
            Protein = 3,
            Fat = 4,
            Description = "one cup",
            ServingAmount = 100,
            ServingUnit = "g",
            ExternalId = "legacy-serving",
        };
        var ingredient = new Ingredient
        {
            Name = "Milk",
            Created = timestamp.AddDays(-1),
            Updated = timestamp,
            ImageUrl = "image",
            ThumbnailUrl = "thumbnail",
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
            Assert.That(response.CachedAt, Is.EqualTo(timestamp));
            Assert.That(response.ImageUrl, Is.EqualTo("image"));
            Assert.That(response.ThumbnailUrl, Is.EqualTo("thumbnail"));
            Assert.That(response.Servings.Single(), Is.EqualTo(new IngredientServingResponse(
                serving.Id.ToString(), "one cup", 100, "g", 10, 2, 3, 4)));
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
