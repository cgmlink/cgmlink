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
}
