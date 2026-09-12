using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.GetMeal;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals.GetMeal;

[TestFixture]
public class GetMealTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    private Meal CreateMeal(Guid id)
    {
        return new Meal
        {
            Id = id,
            UserId = _userId,
            Name = "Breakfast",
            ImageUrl = "https://example.com/image.jpg",
            ThumbnailUrl = "https://example.com/thumb.jpg",
            Calories = 500,
            Carbs = 50,
            Protein = 25,
            Fat = 20,
            Created = DateTimeOffset.UtcNow.AddDays(-1),
        };
    }

    private void SetupMeal(Meal meal)
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Meal_When_Meal_Found()
    {
        var mealId = Guid.NewGuid();
        var meal = CreateMeal(mealId);
        SetupMeal(meal);

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.GetAll(It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<GetMealResponse>>());
        var okResult = result.Result as Ok<GetMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Id, Is.EqualTo(mealId));
            Assert.That(okResult.Value.Name, Is.EqualTo("Breakfast"));
            Assert.That(okResult.Value.Calories, Is.EqualTo(500));
            Assert.That(okResult.Value.Carbs, Is.EqualTo(50));
            Assert.That(okResult.Value.Protein, Is.EqualTo(25));
            Assert.That(okResult.Value.Fat, Is.EqualTo(20));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_IngredientIds_When_Meal_Has_Ingredients()
    {
        var mealId = Guid.NewGuid();
        var meal = CreateMeal(mealId);
        var ingredientId = Guid.NewGuid();
        meal.Ingredients.Add(new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            IngredientId = ingredientId,
            Quantity = 2,
            Created = DateTimeOffset.UtcNow,
        });
        SetupMeal(meal);

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<GetMealResponse>>());
        var okResult = result.Result as Ok<GetMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.IngredientIds, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.IngredientIds, Does.Contain(ingredientId));
        });
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Found()
    {
        var mealId = Guid.NewGuid();

        _mealsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        Assert.That(async () => await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Linked_To_User()
    {
        var mealId = Guid.NewGuid();
        var meal = new Meal
        {
            Id = mealId,
            UserId = Guid.NewGuid(),
            Name = "Breakfast",
            Calories = 500,
            Carbs = 50,
            Protein = 25,
            Fat = 20,
            Created = DateTimeOffset.UtcNow,
        };
        SetupMeal(meal);

        Assert.That(async () => await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Is_Soft_Deleted()
    {
        var mealId = Guid.NewGuid();
        var meal = CreateMeal(mealId);
        meal.Deleted = DateTimeOffset.UtcNow;
        SetupMeal(meal);

        Assert.That(async () => await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        var mealId = Guid.NewGuid();

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
                _mealsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}