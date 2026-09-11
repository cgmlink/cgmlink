using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients.GetIngredient;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Ingredients.GetIngredient;

[TestFixture]
public class GetIngredientTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Ingredient_When_Ingredient_Found()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = ingredientId, Created = DateTimeOffset.UtcNow } },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        var result = await Endpoint.HandleAsync(ingredientId, _currentUserMock.Object,
            _ingredientsRepositoryMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.GetAll(It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<GetIngredientResponse>>());
        var okResult = result.Result as Ok<GetIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Id, Is.EqualTo(ingredientId));
            Assert.That(okResult.Value.Name, Is.EqualTo("Milk"));
            Assert.That(okResult.Value.Barcode, Is.EqualTo("123"));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Servings_When_Ingredient_Has_Servings()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = ingredientId, Created = DateTimeOffset.UtcNow } },
            Servings =
            {
                new IngredientServing
                {
                    Id = Guid.NewGuid(),
                    IngredientId = ingredientId,
                    Description = "1 cup",
                    Calories = 100,
                    Carbs = 10,
                    Protein = 5,
                    Fat = 2,
                    Created = DateTimeOffset.UtcNow,
                }
            },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        var result = await Endpoint.HandleAsync(ingredientId, _currentUserMock.Object,
            _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<GetIngredientResponse>>());
        var okResult = result.Result as Ok<GetIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Servings, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.Servings!.First().Description, Is.EqualTo("1 cup"));
            Assert.That(okResult.Value.Servings!.First().Calories, Is.EqualTo(100));
        });
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Not_Found()
    {
        var ingredientId = Guid.NewGuid();

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Not_Linked_To_User()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = Guid.NewGuid(), IngredientId = ingredientId, Created = DateTimeOffset.UtcNow } },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Is_Soft_Deleted()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            Deleted = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = ingredientId, Created = DateTimeOffset.UtcNow } },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        var ingredientId = Guid.NewGuid();

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}