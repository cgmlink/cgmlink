using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients.UpdateIngredient;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Ingredients.UpdateIngredient;

[TestFixture]
public class UpdateIngredientTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<UpdateIngredientRequest>> _validatorMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<UpdateIngredientRequest>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateIngredientRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

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
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new UpdateIngredientRequest { Name = "" };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Name", "Name is required.") }
            });

        var result = await Endpoint.HandleAsync(Guid.NewGuid(), request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_When_Only_Name_Is_Provided()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        SetupIngredient(ingredient);

        var request = new UpdateIngredientRequest { Name = "Updated Milk" };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ingredient>(i =>
            i.Name == "Updated Milk" &&
            i.Barcode == "123" &&
            i.ImageUrl == "https://example.com/image.jpg" &&
            i.ThumbnailUrl == "https://example.com/thumb.jpg" &&
            i.Updated != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.TypeOf<Ok<UpdateIngredientResponse>>());
            Assert.That(okResult!.Value.Name, Is.EqualTo("Updated Milk"));
            Assert.That(okResult.Value.Barcode, Is.EqualTo("123"));
            Assert.That(okResult.Value.ImageUrl, Is.EqualTo("https://example.com/image.jpg"));
            Assert.That(okResult.Value.ThumbnailUrl, Is.EqualTo("https://example.com/thumb.jpg"));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_And_Not_Change_Any_Values_When_Request_Is_Empty()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        SetupIngredient(ingredient);

        var request = new UpdateIngredientRequest();

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ingredient>(i =>
            i.Name == "Milk" &&
            i.Barcode == "123" &&
            i.ImageUrl == "https://example.com/image.jpg" &&
            i.ThumbnailUrl == "https://example.com/thumb.jpg" &&
            i.Updated != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateIngredientResponse>;
        Assert.That(okResult!.Value.Name, Is.EqualTo("Milk"));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_When_All_Properties_Are_Provided()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        SetupIngredient(ingredient);

        var request = new UpdateIngredientRequest
        {
            Name = "Almond Milk",
            Barcode = "456",
            ImageUrl = "https://example.com/new-image.jpg",
            ThumbnailUrl = "https://example.com/new-thumb.jpg",
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ingredient>(i =>
            i.Name == "Almond Milk" &&
            i.Barcode == "456" &&
            i.ImageUrl == "https://example.com/new-image.jpg" &&
            i.ThumbnailUrl == "https://example.com/new-thumb.jpg"
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateIngredientResponse>;
        Assert.That(okResult!.Value.Name, Is.EqualTo("Almond Milk"));
    }

    [Test]
    public async Task HandleAsync_Should_Replace_Servings_When_Servings_Are_Provided()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        ingredient.Servings.Add(new IngredientServing
        {
            Id = Guid.NewGuid(),
            IngredientId = id,
            Description = "1 cup",
            Calories = 100,
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Created = DateTimeOffset.UtcNow,
        });
        SetupIngredient(ingredient);

        var request = new UpdateIngredientRequest
        {
            Servings =
            [
                new UpdateIngredientRequest.UpdateIngredientServingRequest
                {
                    Description = "100g",
                    Calories = 50,
                    Carbs = 5,
                    Protein = 3,
                    Fat = 1,
                }
            ]
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(ingredient.Servings, Has.Count.EqualTo(1));
            Assert.That(ingredient.Servings.First().Description, Is.EqualTo("100g"));
            Assert.That(ingredient.Servings.First().Calories, Is.EqualTo(50));
        });

        var okResult = result.Result as Ok<UpdateIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Servings, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.Servings!.First().Description, Is.EqualTo("100g"));
            Assert.That(okResult.Value.Servings!.First().Calories, Is.EqualTo(50));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Not_Change_Servings_When_Servings_Are_Not_Provided()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        var existing = new IngredientServing
        {
            Id = Guid.NewGuid(),
            IngredientId = id,
            Description = "1 cup",
            Calories = 100,
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Created = DateTimeOffset.UtcNow,
        };
        ingredient.Servings.Add(existing);
        SetupIngredient(ingredient);

        var request = new UpdateIngredientRequest { Name = "Milk 2%" };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(ingredient.Servings, Has.Count.EqualTo(1));
            Assert.That(ingredient.Servings.First(), Is.SameAs(existing));
        });

        var okResult = result.Result as Ok<UpdateIngredientResponse>;
        Assert.That(okResult!.Value.Servings, Has.Count.EqualTo(1));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Not_Found()
    {
        var id = Guid.NewGuid();

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var request = new UpdateIngredientRequest { Name = "Updated Milk" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_ConflictException_When_Ingredient_Has_ProductId()
    {
        var id = Guid.NewGuid();
        var ingredient = CreateIngredient(id);
        ingredient.ProductId = "product1";
        SetupIngredient(ingredient);

        var request = new UpdateIngredientRequest { Name = "Updated Milk" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None),
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

        var request = new UpdateIngredientRequest { Name = "Updated Milk" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        var request = new UpdateIngredientRequest { Name = "Updated Milk" };

        Assert.That(async () => await Endpoint.HandleAsync(Guid.NewGuid(), request, _validatorMock.Object,
                _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}