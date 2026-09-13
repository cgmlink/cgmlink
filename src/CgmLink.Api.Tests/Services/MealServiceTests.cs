using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using NUnit.Framework;
using System;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public class MealServiceTests
{
    private readonly MealService _service = new();

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