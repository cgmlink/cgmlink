using CgmLink.Api.Services;
using CgmLink.Data.Entities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public class TreatmentServiceTests
{
    private TreatmentService _service;

    [SetUp]
    public void SetUp()
    {
        _service = new TreatmentService();
    }

    [Test]
    public void RecalculateTreatmentNutrition_Should_Return_Zeros_When_No_Links()
    {
        var treatment = CreateTreatment();

        var result = _service.RecalculateTreatmentNutrition(treatment);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.SameAs(treatment));
            Assert.That(treatment.Calories, Is.EqualTo(0m));
            Assert.That(treatment.Carbs, Is.EqualTo(0m));
            Assert.That(treatment.Protein, Is.EqualTo(0m));
            Assert.That(treatment.Fat, Is.EqualTo(0m));
        });
    }

    [Test]
    public void RecalculateTreatmentNutrition_Should_Multiply_Meal_Nutrition_By_Quantity()
    {
        var treatment = CreateTreatment();
        treatment.Meals.Add(CreateTreatmentMeal(CreateMeal(100m, 10m, 5m, 2m), 2m, treatment.Id));

        _service.RecalculateTreatmentNutrition(treatment);

        Assert.Multiple(() =>
        {
            Assert.That(treatment.Calories, Is.EqualTo(200m));
            Assert.That(treatment.Carbs, Is.EqualTo(20m));
            Assert.That(treatment.Protein, Is.EqualTo(10m));
            Assert.That(treatment.Fat, Is.EqualTo(4m));
        });
    }

    [Test]
    public void RecalculateTreatmentNutrition_Should_Sum_Meals_And_Ingredients()
    {
        var treatment = CreateTreatment();
        treatment.Meals.Add(CreateTreatmentMeal(CreateMeal(100m, 10m, 5m, 2m), 2m, treatment.Id));
        treatment.Ingredients.Add(CreateTreatmentIngredient(CreateServing(50m, 5m, 3m, 1m), 1m, treatment.Id));

        _service.RecalculateTreatmentNutrition(treatment);

        Assert.Multiple(() =>
        {
            Assert.That(treatment.Calories, Is.EqualTo(250m));
            Assert.That(treatment.Carbs, Is.EqualTo(25m));
            Assert.That(treatment.Protein, Is.EqualTo(13m));
            Assert.That(treatment.Fat, Is.EqualTo(5m));
        });
    }

    [Test]
    public void RecalculateTreatmentNutrition_Should_Skip_Lines_When_Meal_Not_Loaded()
    {
        var treatment = CreateTreatment();
        treatment.Meals.Add(new TreatmentMeal
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            MealId = Guid.NewGuid(),
            Meal = null,
            Quantity = 2m,
            Created = DateTimeOffset.UtcNow,
        });

        _service.RecalculateTreatmentNutrition(treatment);

        Assert.Multiple(() =>
        {
            Assert.That(treatment.Calories, Is.EqualTo(0m));
            Assert.That(treatment.Carbs, Is.EqualTo(0m));
            Assert.That(treatment.Protein, Is.EqualTo(0m));
            Assert.That(treatment.Fat, Is.EqualTo(0m));
        });
    }

    [Test]
    public void RecalculateTreatmentNutrition_Should_Skip_Lines_When_Serving_Not_Loaded()
    {
        var treatment = CreateTreatment();
        treatment.Ingredients.Add(new TreatmentIngredient
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatment.Id,
            IngredientId = Guid.NewGuid(),
            ServingId = Guid.NewGuid(),
            Serving = null,
            Quantity = 3m,
            Created = DateTimeOffset.UtcNow,
        });

        _service.RecalculateTreatmentNutrition(treatment);

        Assert.Multiple(() =>
        {
            Assert.That(treatment.Calories, Is.EqualTo(0m));
            Assert.That(treatment.Carbs, Is.EqualTo(0m));
            Assert.That(treatment.Protein, Is.EqualTo(0m));
            Assert.That(treatment.Fat, Is.EqualTo(0m));
        });
    }

    [Test]
    public void CreateTreatment_Should_Build_Links_And_Calculate_Nutrition()
    {
        var created = DateTimeOffset.UtcNow.AddMinutes(-5);
        var userId = Guid.NewGuid();
        var injectionId = Guid.NewGuid();
        var readingId = Guid.NewGuid();
        var meal = CreateMeal(100m, 10m, 5m, 2m);
        var serving = CreateServing(50m, 5m, 3m, 1m);
        var ingredient = CreateIngredient(serving);
        var mealLookup = new Dictionary<Guid, Meal> { [meal.Id] = meal };
        var ingredientLookup = new Dictionary<Guid, Ingredient> { [ingredient.Id] = ingredient };

        var result = _service.CreateTreatment(
            [new RequestMeal(meal.Id, 2m)],
            [new RequestIngredient(ingredient.Id, serving.Id, 1m)],
            mealLookup,
            ingredientLookup,
            userId,
            readingId,
            injectionId,
            created);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.UserId, Is.EqualTo(userId));
            Assert.That(result.ReadingId, Is.EqualTo(readingId));
            Assert.That(result.InjectionId, Is.EqualTo(injectionId));
            Assert.That(result.Created, Is.EqualTo(created));
            Assert.That(result.Calories, Is.EqualTo(250m));
            Assert.That(result.Carbs, Is.EqualTo(25m));
            Assert.That(result.Protein, Is.EqualTo(13m));
            Assert.That(result.Fat, Is.EqualTo(5m));
            Assert.That(result.Meals, Has.Count.EqualTo(1));
            Assert.That(result.Meals.Single().MealId, Is.EqualTo(meal.Id));
            Assert.That(result.Meals.Single().Meal, Is.SameAs(meal));
            Assert.That(result.Meals.Single().Quantity, Is.EqualTo(2m));
            Assert.That(result.Meals.Single().TreatmentId, Is.EqualTo(result.Id));
            Assert.That(result.Ingredients, Has.Count.EqualTo(1));
            Assert.That(result.Ingredients.Single().IngredientId, Is.EqualTo(ingredient.Id));
            Assert.That(result.Ingredients.Single().Ingredient, Is.SameAs(ingredient));
            Assert.That(result.Ingredients.Single().ServingId, Is.EqualTo(serving.Id));
            Assert.That(result.Ingredients.Single().Serving, Is.SameAs(serving));
            Assert.That(result.Ingredients.Single().Quantity, Is.EqualTo(1m));
            Assert.That(result.Ingredients.Single().TreatmentId, Is.EqualTo(result.Id));
        });
    }

    [Test]
    public void CreateTreatment_Should_Return_Zero_Nutrition_When_No_Meals_Or_Ingredients()
    {
        var result = _service.CreateTreatment(
            [],
            [],
            new Dictionary<Guid, Meal>(),
            new Dictionary<Guid, Ingredient>(),
            Guid.NewGuid(),
            null,
            null,
            DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(result.Calories, Is.EqualTo(0m));
            Assert.That(result.Carbs, Is.EqualTo(0m));
            Assert.That(result.Protein, Is.EqualTo(0m));
            Assert.That(result.Fat, Is.EqualTo(0m));
            Assert.That(result.Meals, Is.Empty);
            Assert.That(result.Ingredients, Is.Empty);
        });
    }

    private static Treatment CreateTreatment()
    {
        return new Treatment
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private static Meal CreateMeal(decimal calories, decimal carbs, decimal protein, decimal fat)
    {
        return new Meal
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Name = "Breakfast",
            Calories = calories,
            Carbs = carbs,
            Protein = protein,
            Fat = fat,
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

    private static Ingredient CreateIngredient(IngredientServing serving)
    {
        return new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Servings = { serving },
        };
    }

    private static TreatmentMeal CreateTreatmentMeal(Meal meal, decimal quantity, Guid treatmentId)
    {
        return new TreatmentMeal
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            MealId = meal.Id,
            Meal = meal,
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private static TreatmentIngredient CreateTreatmentIngredient(IngredientServing serving, decimal quantity, Guid treatmentId)
    {
        return new TreatmentIngredient
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            IngredientId = Guid.NewGuid(),
            ServingId = serving.Id,
            Serving = serving,
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private sealed record RequestMeal(Guid MealId, decimal Quantity) : ITreatmentMealRequest;

    private sealed record RequestIngredient(Guid IngredientId, Guid ServingId, decimal Quantity) : IMealIngredientRequest;
}