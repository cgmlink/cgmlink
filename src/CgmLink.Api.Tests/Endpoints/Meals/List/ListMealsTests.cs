using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.List;
using CgmLink.Api.Models;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals.List;

[TestFixture]
public class ListMealsTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<ListMealsRequest>> _validatorMock;
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<ListMealsRequest>>();
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    private static Meal CreateMeal(string name, int ingredientCount = 0, DateTimeOffset? created = null)
    {
        var ingredients = new List<MealIngredient>();
        for (var i = 0; i < ingredientCount; i++)
        {
            ingredients.Add(new MealIngredient
            {
                MealId = Guid.NewGuid(),
                IngredientId = Guid.NewGuid(),
                Quantity = 1,
                Created = DateTimeOffset.UtcNow,
            });
        }

        return new Meal
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = name,
            Created = created ?? DateTimeOffset.UtcNow,
            Calories = 100,
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Ingredients = ingredients,
        };
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10 };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Page", "Page is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new ListMealsRequest { Page = 0, PageSize = 10 };

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Meals_When_Request_Is_Valid()
    {
        var meals = new List<Meal>
        {
            CreateMeal("Breakfast", ingredientCount: 2),
        };

        _mealsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(meals.AsQueryable());

        _mealsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meals.Count);

        var request = new ListMealsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<ListMealsResponse>>());
        var okResult = result.Result as Ok<ListMealsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Meals.Count, Is.EqualTo(1));
            Assert.That(okResult.Value.Meals.First().IngredientCount, Is.EqualTo(2));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(1));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Empty_Meals_When_No_Meals_Exist()
    {
        _mealsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(Enumerable.Empty<Meal>().AsQueryable());

        _mealsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListMealsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListMealsResponse>>());
        var okResult = result.Result as Ok<ListMealsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Meals, Is.Empty);
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Exclude_Soft_Deleted_Meals_When_Request_Is_Valid()
    {
        var deleted = CreateMeal("Deleted");
        deleted.Deleted = DateTimeOffset.UtcNow;

        Expression<Func<Meal, bool>> predicate = null;

        _mealsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Callback<Expression<Func<Meal, bool>>, FindOptions>((expression, _) => predicate = expression)
            .Returns(new[] { deleted }.AsQueryable());

        _mealsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var request = new ListMealsRequest { Page = 0, PageSize = 10 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(predicate, Is.Not.Null);
        Assert.That(predicate.Compile()(deleted), Is.False);
        Assert.That(result.Result, Is.TypeOf<Ok<ListMealsResponse>>());
    }

    [Test]
    public async Task HandleAsync_Should_Paginate_Meals()
    {
        var meals = new List<Meal>
        {
            CreateMeal("Breakfast", created: DateTimeOffset.UtcNow.AddHours(-2)),
            CreateMeal("Lunch", created: DateTimeOffset.UtcNow.AddHours(-1)),
        };

        _mealsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(meals.AsQueryable());

        _mealsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meals.Count);

        var request = new ListMealsRequest { Page = 1, PageSize = 1 };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListMealsResponse>>());
        var okResult = result.Result as Ok<ListMealsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Meals.Count, Is.EqualTo(1));
            Assert.That(okResult.Value.Meals.First().Name, Is.EqualTo("Breakfast"));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Sort_By_Requested_Field_Ascending()
    {
        var meals = new List<Meal>
        {
            CreateMeal("Zucchini"),
            CreateMeal("Apple"),
        };

        _mealsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(meals.AsQueryable());

        _mealsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meals.Count);

        var request = new ListMealsRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = nameof(Meal.Name),
            SortDirection = SortDirection.Asc,
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListMealsResponse>>());
        var okResult = result.Result as Ok<ListMealsResponse>;
        Assert.That(okResult!.Value.Meals.Select(m => m.Name), Is.EqualTo(new[] { "Apple", "Zucchini" }));
    }

    [Test]
    public async Task HandleAsync_Should_Sort_By_Requested_Field_Descending()
    {
        var meals = new List<Meal>
        {
            CreateMeal("Apple"),
            CreateMeal("Zucchini"),
        };

        _mealsRepositoryMock
            .Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(meals.AsQueryable());

        _mealsRepositoryMock
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(meals.Count);

        var request = new ListMealsRequest
        {
            Page = 0,
            PageSize = 10,
            SortBy = nameof(Meal.Name),
            SortDirection = SortDirection.Desc,
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<ListMealsResponse>>());
        var okResult = result.Result as Ok<ListMealsResponse>;
        Assert.That(okResult!.Value.Meals.Select(m => m.Name), Is.EqualTo(new[] { "Zucchini", "Apple" }));
    }
}