using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients.DeleteIngredient;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class DeleteIngredientTests
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

        _ingredientsRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Ingredient CreateIngredient(Guid id)
    {
        return new Ingredient
        {
            Id = id,
            Name = "Milk",
            Barcode = "123",
            ImageUrl = "https://example.com/image.jpg",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            Created = DateTimeOffset.UtcNow.AddDays(-1),
            Users = { new UserIngredient { UserId = _userId, IngredientId = id, Created = DateTimeOffset.UtcNow } },
        };
    }

    private void SetupIngredient(Ingredient ingredient)
    {
        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));
    }

    [Test]
    public async Task HandleAsync_Should_Return_NoContent_When_Ingredient_Is_Deleted()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        SetupIngredient(ingredient);

        var result = await Endpoint.HandleAsync(id, _currentUserMock.Object,
            _ingredientsRepositoryMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ingredient>(i =>
            i.Deleted != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<NoContent>());
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Not_Found()
    {
        var id = Guid.NewGuid();

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Not_Linked_To_User()
    {
        var id = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = id,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = Guid.NewGuid(), IngredientId = id, Created = DateTimeOffset.UtcNow } },
        };
        SetupIngredient(ingredient);

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_ConflictException_When_Ingredient_Has_ProductId()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        ingredient.ProductId = "product1";
        SetupIngredient(ingredient);

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<ConflictException>().With.Message.EqualTo("INGREDIENT_READ_ONLY"));

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Is_Soft_Deleted()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        ingredient.Deleted = DateTimeOffset.UtcNow;
        SetupIngredient(ingredient);

        Assert.That(async () => await Endpoint.HandleAsync(id, _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_ALREADY_DELETED"));

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(Guid.NewGuid(), _currentUserMock.Object,
                _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}