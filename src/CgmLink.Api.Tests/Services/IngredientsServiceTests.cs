using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public class IngredientsServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private IngredientsService _service;

    [SetUp]
    public void SetUp()
    {
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _service = new IngredientsService(_ingredientsRepositoryMock.Object);
    }

    private Ingredient CreateIngredient(Guid id)
    {
        return new Ingredient
        {
            Id = id,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = id, Created = DateTimeOffset.UtcNow } },
        };
    }

    private IngredientServing CreateServing(Guid id, Guid ingredientId)
    {
        return new IngredientServing
        {
            Id = id,
            IngredientId = ingredientId,
            Description = "1 cup",
            Calories = 100,
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private void SetupIngredients(IEnumerable<Ingredient> ingredients)
    {
        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(ingredients.ToList()));
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Return_Lookup_When_Ingredients_Are_Valid()
    {
        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId, ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        var result = await _service.GetValidatedIngredientsAsync(
            [new RequestIngredient(ingredientId, servingId, 2m)], _userId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Keys, Does.Contain(ingredientId));
            Assert.That(result[ingredientId].Servings.Single().Id, Is.EqualTo(servingId));
        });
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Throw_BadRequest_When_Ingredient_Not_Found()
    {
        SetupIngredients(new List<Ingredient>());

        Assert.That(async () => await _service.GetValidatedIngredientsAsync(
                [new RequestIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Throw_BadRequest_When_Serving_Not_Found_On_Ingredient()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(Guid.NewGuid(), ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        Assert.That(async () => await _service.GetValidatedIngredientsAsync(
                [new RequestIngredient(ingredientId, Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Throw_BadRequest_When_Ingredient_Not_Linked_To_User()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = Guid.NewGuid(), IngredientId = ingredientId, Created = DateTimeOffset.UtcNow } },
        };
        SetupIngredients(new List<Ingredient> { ingredient });

        Assert.That(async () => await _service.GetValidatedIngredientsAsync(
                [new RequestIngredient(ingredientId, Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    private sealed record RequestIngredient(Guid IngredientId, Guid ServingId, decimal Quantity) : IMealIngredientRequest;
}