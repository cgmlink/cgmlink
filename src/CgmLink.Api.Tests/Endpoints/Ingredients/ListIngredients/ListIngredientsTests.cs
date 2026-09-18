using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients.ListIngredients;
using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class ListIngredientsTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<ListIngredientsRequest>> _validatorMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<ListIngredientsRequest>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Page", "Page is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Ingredients_When_Request_Is_Valid()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient
            {
                Id = Guid.NewGuid(),
                Name = "Milk",
                Barcode = "123",
                Created = DateTimeOffset.UtcNow,
                Users = { new UserIngredient { UserId = _userId, IngredientId = Guid.NewGuid(), Created = DateTimeOffset.UtcNow } },
            }
        };

        _ingredientsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(ingredients.AsQueryable());

        _ingredientsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredients.Count);

        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<ListIngredientsResponse>>());
        var okResult = result.Result as Ok<ListIngredientsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Ingredients.Count, Is.EqualTo(1));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Empty_Ingredients_When_No_Ingredients_Linked()
    {
        _ingredientsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(Enumerable.Empty<Ingredient>().AsQueryable());

        _ingredientsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListIngredientsResponse>>());
        var okResult = result.Result as Ok<ListIngredientsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Ingredients, Is.Empty);
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Exclude_Soft_Deleted_Ingredients_When_Request_Is_Valid()
    {
        var deleted = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            Deleted = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = Guid.NewGuid(), Created = DateTimeOffset.UtcNow } },
        };

        Expression<Func<Ingredient, bool>> predicate = null;

        _ingredientsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Callback<Expression<Func<Ingredient, bool>>, FindOptions>((expression, _) => predicate = expression)
            .Returns(new[] { deleted }.AsQueryable());

        _ingredientsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListIngredientsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(predicate, Is.Not.Null);
        Assert.That(predicate.Compile()(deleted), Is.False);
        Assert.That(result.Result, Is.TypeOf<Ok<ListIngredientsResponse>>());
    }

    [Test]
    public async Task HandleAsync_Should_Paginate_Ingredients()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Milk", Created = DateTimeOffset.UtcNow.AddHours(-2) },
            new Ingredient { Id = Guid.NewGuid(), Name = "Eggs", Created = DateTimeOffset.UtcNow.AddHours(-1) },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(ingredients.AsQueryable());

        _ingredientsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredients.Count);

        var request = new ListIngredientsRequest { Page = 1, PageSize = 1, SortDirection = SortDirection.Desc };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListIngredientsResponse>>());
        var okResult = result.Result as Ok<ListIngredientsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Ingredients.Count, Is.EqualTo(1));
            Assert.That(okResult.Value.Ingredients.First().Name, Is.EqualTo("Milk"));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Sort_By_Requested_Field_Ascending()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Zebra", Created = DateTimeOffset.UtcNow },
            new Ingredient { Id = Guid.NewGuid(), Name = "Apple", Created = DateTimeOffset.UtcNow },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(ingredients.AsQueryable());

        _ingredientsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredients.Count);

        var request = new ListIngredientsRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = nameof(Ingredient.Name),
            SortDirection = SortDirection.Asc,
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListIngredientsResponse>>());
        var okResult = result.Result as Ok<ListIngredientsResponse>;
        Assert.That(okResult!.Value.Ingredients.Select(i => i.Name), Is.EqualTo(new[] { "Apple", "Zebra" }));
    }

    [Test]
    public async Task HandleAsync_Should_Sort_By_Requested_Field_Descending()
    {
        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), Name = "Apple", Created = DateTimeOffset.UtcNow },
            new Ingredient { Id = Guid.NewGuid(), Name = "Zebra", Created = DateTimeOffset.UtcNow },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(ingredients.AsQueryable());

        _ingredientsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ingredients.Count);

        var request = new ListIngredientsRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = nameof(Ingredient.Name),
            SortDirection = SortDirection.Desc,
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _ingredientsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListIngredientsResponse>>());
        var okResult = result.Result as Ok<ListIngredientsResponse>;
        Assert.That(okResult!.Value.Ingredients.Select(i => i.Name), Is.EqualTo(new[] { "Zebra", "Apple" }));
    }
}