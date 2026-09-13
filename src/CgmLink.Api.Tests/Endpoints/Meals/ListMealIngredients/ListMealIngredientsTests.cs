using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.ListMealIngredients;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals.ListMealIngredients;

[TestFixture]
public class ListMealIngredientsTests
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

    private MealIngredient CreateMealIngredient(Guid mealId, Guid ingredientId, IngredientServing? serving, decimal quantity)
    {
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
        };

        return new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            IngredientId = ingredientId,
            Ingredient = ingredient,
            ServingId = serving?.Id,
            Serving = serving,
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private IngredientServing CreateServing(Guid ingredientId, decimal calories, decimal carbs, decimal protein, decimal fat)
    {
        return new IngredientServing
        {
            Id = Guid.NewGuid(),
            IngredientId = ingredientId,
            Description = "1 cup",
            Calories = calories,
            Carbs = carbs,
            Protein = protein,
            Fat = fat,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private void SetupMeal(Meal meal)
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));
    }

    private Meal CreateMeal(Guid id, List<MealIngredient>? ingredients = null)
    {
        var meal = new Meal
        {
            Id = id,
            UserId = _userId,
            Name = "Breakfast",
            Calories = 500,
            Carbs = 50,
            Protein = 25,
            Fat = 20,
            Created = DateTimeOffset.UtcNow.AddDays(-1),
        };

        foreach (var mealIngredient in ingredients ?? [])
        {
            meal.Ingredients.Add(mealIngredient);
        }

        return meal;
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Ingredients_When_Meal_Found()
    {
        var mealId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var serving = CreateServing(ingredientId, 100m, 10m, 5m, 2m);
        var mealIngredient = CreateMealIngredient(mealId, ingredientId, serving, 2m);
        SetupMeal(CreateMeal(mealId, new List<MealIngredient> { mealIngredient }));

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.GetAll(It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<ListMealIngredientsResponse>>());
        var okResult = result.Result as Ok<ListMealIngredientsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Ingredients, Has.Count.EqualTo(1));

            var response = okResult.Value.Ingredients.Single();
            Assert.That(response.IngredientId, Is.EqualTo(ingredientId));
            Assert.That(response.IngredientName, Is.EqualTo("Milk"));
            Assert.That(response.Quantity, Is.EqualTo(2));
            Assert.That(response.Serving, Is.Not.Null);
            Assert.That(response.Serving!.Description, Is.EqualTo("1 cup"));
            Assert.That(response.Calories, Is.EqualTo(200));
            Assert.That(response.Carbs, Is.EqualTo(20));
            Assert.That(response.Protein, Is.EqualTo(10));
            Assert.That(response.Fat, Is.EqualTo(4));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_All_Ingredients_When_Meal_Has_Multiple_Ingredients()
    {
        var mealId = Guid.NewGuid();
        var ingredientId1 = Guid.NewGuid();
        var ingredientId2 = Guid.NewGuid();
        var mealIngredient1 = CreateMealIngredient(mealId, ingredientId1, CreateServing(ingredientId1, 100m, 10m, 5m, 2m), 2m);
        var mealIngredient2 = CreateMealIngredient(mealId, ingredientId2, CreateServing(ingredientId2, 50m, 5m, 3m, 1m), 1m);
        SetupMeal(CreateMeal(mealId, new List<MealIngredient> { mealIngredient1, mealIngredient2 }));

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListMealIngredientsResponse>;
        Assert.That(okResult!.Value.Ingredients.Count, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(okResult.Value.Ingredients.First().IngredientId, Is.EqualTo(ingredientId1));
            Assert.That(okResult.Value.Ingredients.First().Calories, Is.EqualTo(200));
            Assert.That(okResult.Value.Ingredients.Last().IngredientId, Is.EqualTo(ingredientId2));
            Assert.That(okResult.Value.Ingredients.Last().Calories, Is.EqualTo(50));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Empty_Ingredients_When_Meal_Has_No_Ingredients()
    {
        var mealId = Guid.NewGuid();
        SetupMeal(CreateMeal(mealId));

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListMealIngredientsResponse>;
        Assert.That(okResult!.Value.Ingredients, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_Should_Return_Zero_Nutrition_When_Meal_Ingredient_Has_No_Serving()
    {
        var mealId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var mealIngredient = CreateMealIngredient(mealId, ingredientId, null, 2m);
        SetupMeal(CreateMeal(mealId, new List<MealIngredient> { mealIngredient }));

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListMealIngredientsResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Ingredients.Single().Serving, Is.Null);
            Assert.That(okResult.Value.Ingredients.Single().Calories, Is.EqualTo(0));
            Assert.That(okResult.Value.Ingredients.Single().Carbs, Is.EqualTo(0));
            Assert.That(okResult.Value.Ingredients.Single().Protein, Is.EqualTo(0));
            Assert.That(okResult.Value.Ingredients.Single().Fat, Is.EqualTo(0));
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