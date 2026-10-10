using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Nutrition;
using CgmLink.Nutrition.Source;
using CatalogServing = CgmLink.Nutrition.Source.NutritionServing;
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
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private MealService _service;
    private Mock<INutritionCatalog> _nutritionCatalogMock;

    [SetUp]
    public void SetUp()
    {
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _nutritionCatalogMock = new Mock<INutritionCatalog>();
        _nutritionCatalogMock.SetupGet(c => c.Source).Returns("external");
        _service = new MealService(_mealsRepositoryMock.Object, _nutritionCatalogMock.Object, Mock.Of<IIngredientsService>(), Mock.Of<IRepository<NutritionIngredient>>());
    }

    [Test]
    public async Task RecalculateNutrition_Should_Return_Zeros_When_Meal_Has_No_Ingredients()
    {
        var meal = CreateMeal();

        var result = await _service.RecalculateMealsNutritionAsync(meal);

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
    public async Task RecalculateNutrition_Should_Multiply_Serving_Nutrition_By_Quantity()
    {
        var meal = CreateMeal();
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m));

        var result = await _service.RecalculateMealsNutritionAsync(meal);

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
    public async Task RecalculateNutrition_Should_Sum_Multiple_Ingredient_Lines()
    {
        var meal = CreateMeal();
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m));
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(50m, 5m, 3m, 1m), 1m));

        await _service.RecalculateMealsNutritionAsync(meal);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Calories, Is.EqualTo(250m));
            Assert.That(meal.Carbs, Is.EqualTo(25m));
            Assert.That(meal.Protein, Is.EqualTo(13m));
            Assert.That(meal.Fat, Is.EqualTo(5m));
        });
    }

    [Test]
    public async Task RecalculateNutrition_Should_Skip_Lines_When_Serving_Not_Loaded()
    {
        var meal = CreateMeal();
        meal.Ingredients.Add(CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m));
        meal.Ingredients.Add(new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = meal.Id,
            Meal = meal,
            IngredientId = Guid.NewGuid(),
            ServingId = Guid.NewGuid(),
            Serving = null,
            Quantity = 3m,
            Created = DateTimeOffset.UtcNow,
        });

        await _service.RecalculateMealsNutritionAsync(meal);

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

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal, otherMeal }));

        await _service.RecalculateMealsWithIngredientNutrition(ingredientId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Calories, Is.EqualTo(200m));
            Assert.That(meal.Carbs, Is.EqualTo(20m));
            Assert.That(meal.Protein, Is.EqualTo(10m));
            Assert.That(meal.Fat, Is.EqualTo(4m));
            Assert.That(otherMeal.Calories, Is.EqualTo(0m));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RecalculateNutrition_Should_Include_External_Servings(bool recalculateForIngredient)
    {
        var meal = CreateMeal();
        var local = CreateMealIngredient(CreateServing(100m, 10m, 5m, 2m), 2m);
        meal.Ingredients.Add(local);
        meal.NutritionIngredients.Add(CreateExternalIngredient("product", "cup", 1.5m));
        meal.NutritionIngredients.Add(CreateExternalIngredient("product", "spoon", 2m));
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _nutritionCatalogMock.Setup(c => c.GetAsync("product", token)).ReturnsAsync(new NutritionProduct
        {
            ProductId = "product",
            Name = "External food",
            Servings = [
                new CatalogServing { ExternalId = "cup", Calories = 80m, Carbs = 8m, Protein = 4m, Fat = 2m },
                new CatalogServing { ExternalId = "spoon", Calories = 10m, Carbs = 1m, Protein = 0.5m, Fat = 0.25m },
            ],
        });

        if (recalculateForIngredient)
        {
            _mealsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new TestAsyncEnumerable<Meal>(new[] { meal }));
            await _service.RecalculateMealsWithIngredientNutrition(local.IngredientId, token);
        }
        else
        {
            var result = await _service.RecalculateMealsNutritionAsync(meal, token);
            Assert.That(result, Is.SameAs(meal));
        }

        Assert.Multiple(() =>
        {
            Assert.That(meal.Calories, Is.EqualTo(340m));
            Assert.That(meal.Carbs, Is.EqualTo(34m));
            Assert.That(meal.Protein, Is.EqualTo(17m));
            Assert.That(meal.Fat, Is.EqualTo(7.5m));
        });
        _nutritionCatalogMock.Verify(c => c.GetAsync("product", token), Times.Once);
    }

    [TestCase("product", "INGREDIENT_ID_INVALID")]
    [TestCase("serving", "INGREDIENT_SERVING_ID_INVALID")]
    [TestCase("servingIdentity", "INGREDIENT_SERVING_ID_INVALID")]
    [TestCase("source", "INGREDIENT_ID_INVALID")]
    public void RecalculateNutrition_Should_Reject_Unresolvable_External_Ingredients(string missing, string expectedError)
    {
        var meal = CreateMeal();
        meal.Calories = 123m;
        var external = CreateExternalIngredient("product", "cup", 2m);
        meal.NutritionIngredients.Add(external);
        if (missing == "source")
        {
            external.NutritionIngredient!.Source = "unsupported";
        }
        if (missing == "servingIdentity")
        {
            external.Serving = null;
        }
        if (missing != "product")
        {
            _nutritionCatalogMock.Setup(c => c.GetAsync("product", CancellationToken.None))
                .ReturnsAsync(new NutritionProduct { ProductId = "product", Name = "External food" });
        }

        Assert.That(async () => await _service.RecalculateMealsNutritionAsync(meal),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo(expectedError));
        Assert.That(meal.Calories, Is.EqualTo(123m));
        if (missing == "source")
        {
            _nutritionCatalogMock.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    private static MealNutritionIngredient CreateExternalIngredient(string productId, string servingId, decimal quantity)
    {
        var ingredient = new NutritionIngredient { Source = "external", ProductId = productId };
        var serving = new CgmLink.Data.Entities.NutritionServing
        {
            NutritionIngredient = ingredient,
            NutritionIngredientId = ingredient.Id,
            ServingId = servingId,
        };
        return new MealNutritionIngredient
        {
            NutritionIngredientId = ingredient.Id,
            NutritionIngredient = ingredient,
            ServingId = serving.Id,
            Serving = serving,
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
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

    [Test]
    public async Task GetValidatedMealsAsync_Should_Return_Lookup_When_Meals_Are_Valid()
    {
        var meal = CreateMeal();
        meal.UserId = _userId;

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));

        var result = await _service.GetValidatedMealsAsync(
            [new RequestMeal(meal.Id, 2m)], _userId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Keys, Does.Contain(meal.Id));
            Assert.That(result[meal.Id], Is.SameAs(meal));
        });
    }

    [Test]
    public async Task GetValidatedMealsAsync_Should_Throw_BadRequest_When_Meal_Not_Found()
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        Assert.That(async () => await _service.GetValidatedMealsAsync(
                [new RequestMeal(Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("MEAL_ID_INVALID"));
    }

    [Test]
    public async Task GetValidatedMealsAsync_Should_Throw_BadRequest_When_Meal_Not_Linked_To_User()
    {
        var meal = CreateMeal();
        meal.UserId = Guid.NewGuid();

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));

        Assert.That(async () => await _service.GetValidatedMealsAsync(
                [new RequestMeal(meal.Id, 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("MEAL_ID_INVALID"));
    }

    [Test]
    public async Task GetValidatedMealsAsync_Should_Throw_BadRequest_When_Meal_Is_Soft_Deleted()
    {
        var meal = CreateMeal();
        meal.UserId = _userId;
        meal.Deleted = DateTimeOffset.UtcNow;

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));

        Assert.That(async () => await _service.GetValidatedMealsAsync(
                [new RequestMeal(meal.Id, 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("MEAL_ID_INVALID"));
    }

    private readonly Guid _userId = Guid.NewGuid();

    private sealed record RequestMeal(Guid MealId, decimal Quantity) : ITreatmentMealRequest;

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

    private static MealIngredient CreateMealIngredient(IngredientServing serving, decimal quantity)
    {
        return new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = Guid.NewGuid(),
            IngredientId = Guid.NewGuid(),
            ServingId = serving.Id,
            Serving = serving,
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }
}
