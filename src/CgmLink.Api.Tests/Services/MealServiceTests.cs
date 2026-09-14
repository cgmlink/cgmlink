using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public class MealServiceTests
{
    private Mock<IRepository<MealIngredient>> _mealIngredientsRepositoryMock;
    private MealService _service;

    [SetUp]
    public void SetUp()
    {
        _mealIngredientsRepositoryMock = new Mock<IRepository<MealIngredient>>();
        _service = new MealService(_mealIngredientsRepositoryMock.Object);
    }

    [Test]
    public void RecalculateNutrition_Should_Return_Zeros_When_Meal_Has_No_Ingredients()
    {
        var meal = CreateMeal();

        var result = _service.RecalculateNutrition(meal);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(meal));
            Assert.That(meal.Calories, Is.EqualTo(0m));
            Assert.That(meal.Carbs, Is.EqualTo(0m));
            Assert.That(meal.Protein, Is.EqualTo(0m));
            Assert.That(meal.Fat, Is.EqualTo(0m));
        });
    }

    [Test]
    public void RecalculateNutrition_Should_Multiply_Serving_Nutrition_By_Quantity()
    {
        var meal = CreateMeal();
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m));

        var result = _service.RecalculateNutrition(meal);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(meal));
            Assert.That(meal.Calories, Is.EqualTo(200m));
            Assert.That(meal.Carbs, Is.EqualTo(20m));
            Assert.That(meal.Protein, Is.EqualTo(10m));
            Assert.That(meal.Fat, Is.EqualTo(4m));
        });
    }

    [Test]
    public void RecalculateNutrition_Should_Sum_Multiple_Ingredient_Lines()
    {
        var meal = CreateMeal();
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m));
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(50m, 5m, 3m, 1m), 1m));

        _service.RecalculateNutrition(meal);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Calories, Is.EqualTo(250m));
            Assert.That(meal.Carbs, Is.EqualTo(25m));
            Assert.That(meal.Protein, Is.EqualTo(13m));
            Assert.That(meal.Fat, Is.EqualTo(5m));
        });
    }

    [Test]
    public void RecalculateNutrition_Should_Skip_Lines_Without_A_Serving()
    {
        var meal = CreateMeal();
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m));
        meal.Ingredients.Add(CreateMealIngredient(null, 3m));

        _service.RecalculateNutrition(meal);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Calories, Is.EqualTo(200m));
            Assert.That(meal.Carbs, Is.EqualTo(20m));
            Assert.That(meal.Protein, Is.EqualTo(10m));
            Assert.That(meal.Fat, Is.EqualTo(4m));
        });
    }

    [Test]
    public async Task RecalculateNutritionForIngredient_Should_Recalculate_Meals_Containing_The_Ingredient()
    {
        var ingredientId = Guid.NewGuid();
        var meal = CreateMeal();
        var serving = CreateServing(100m, 10m, 5m, 2m);
        var mealIngredient = new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = meal.Id,
            Meal = meal,
            IngredientId = ingredientId,
            ServingId = serving.Id,
            Serving = serving,
            Quantity = 2m,
            Created = DateTimeOffset.UtcNow,
        };
        meal.Ingredients.Add(mealIngredient);

        var otherMeal = CreateMeal();
        var otherMealIngredient = CreateMealIngredient(CreateServing(50m, 5m, 3m, 1m), 1m);
        otherMealIngredient.Meal = otherMeal;
        otherMeal.Ingredients.Add(otherMealIngredient);

        _mealIngredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<MealIngredient>(new List<MealIngredient> { mealIngredient, otherMealIngredient }));

        await _service.RecalculateNutritionForIngredient(ingredientId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Calories, Is.EqualTo(200m));
            Assert.That(meal.Carbs, Is.EqualTo(20m));
            Assert.That(meal.Protein, Is.EqualTo(10m));
            Assert.That(meal.Fat, Is.EqualTo(4m));
            Assert.That(otherMeal.Calories, Is.EqualTo(0m));
        });
    }

    private static Meal CreateMeal()
    {
        return new Meal
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Breakfast",
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private static IngredientServing CreateServing(decimal calories, decimal carbs, decimal protein, decimal fat)
    {
        return new IngredientServing
        {
            Id = Guid.NewGuid(),
            Description = "1 cup",
            Calories = calories,
            Carbs = carbs,
            Protein = protein,
            Fat = fat,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private static MealIngredient CreateMealIngredient(IngredientServing? serving, decimal quantity)
    {
        return new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = Guid.NewGuid(),
            IngredientId = Guid.NewGuid(),
            ServingId = serving?.Id,
            Serving = serving,
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }
}