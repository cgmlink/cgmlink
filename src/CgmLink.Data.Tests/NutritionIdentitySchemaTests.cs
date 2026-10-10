using CgmLink.Data.Entities;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CgmLink.Data.Tests;

[TestFixture]
internal sealed class NutritionIdentitySchemaTests
{
    private CgmLinkDbContext _dbContext = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<CgmLinkDbContext>()
            .UseSqlServer("Server=unused;Database=unused")
            .Options;

        _dbContext = new CgmLinkDbContext(options);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public void NutritionIngredient_Stores_Only_Identity_Data()
    {
        var entity = _dbContext.Model.FindEntityType(typeof(NutritionIngredient))!;

        Assert.That(entity.GetProperties().Select(property => property.Name),
            Is.EquivalentTo(new[] { nameof(NutritionIngredient.Id), nameof(NutritionIngredient.Source), nameof(NutritionIngredient.ProductId) }));
    }

    [Test]
    public void NutritionServing_Stores_Only_Identity_Data()
    {
        var entity = _dbContext.Model.FindEntityType(typeof(NutritionServing))!;
        var foreignKey = entity.GetForeignKeys().Single();

        Assert.Multiple(() =>
        {
            Assert.That(entity.GetProperties().Select(property => property.Name),
                Is.EquivalentTo(new[] { nameof(NutritionServing.Id), nameof(NutritionServing.NutritionIngredientId), nameof(NutritionServing.ServingId) }));
            Assert.That(foreignKey.IsRequired, Is.True);
        });
    }

    [Test]
    public void MealNutritionIngredient_Requires_Meal_Ingredient_And_Serving()
    {
        var entity = _dbContext.Model.FindEntityType(typeof(MealNutritionIngredient))!;
        var foreignKeys = entity.GetForeignKeys().ToList();
        var meal = _dbContext.Model.FindEntityType(typeof(Meal))!;

        Assert.Multiple(() =>
        {
            Assert.That(entity.GetTableName(), Is.EqualTo("meal_nutrition_ingredients"));
            Assert.That(foreignKeys.Select(key => key.PrincipalEntityType.ClrType),
                Is.EquivalentTo(new[] { typeof(Meal), typeof(NutritionIngredient), typeof(NutritionServing) }));
            Assert.That(foreignKeys.All(key => key.IsRequired), Is.True);
            Assert.That(foreignKeys.Single(key => key.PrincipalEntityType.ClrType == typeof(NutritionServing)).DeleteBehavior,
                Is.EqualTo(DeleteBehavior.NoAction));
            Assert.That(meal.FindNavigation(nameof(Meal.NutritionIngredients))!.TargetEntityType, Is.EqualTo(entity));
        });
    }

    [Test]
    public void Ingredient_No_Longer_Stores_ProductId()
    {
        var entity = _dbContext.Model.FindEntityType(typeof(Ingredient))!;

        Assert.That(entity.FindProperty("ProductId"), Is.Null);
    }
}
