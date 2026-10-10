using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.GetMeal;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals;

[TestFixture]
public class GetMealIngredientTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IIngredientsService> _ingredientsServiceMock;
    private IMealService _mealService;

    [SetUp]
    public void SetUp()
    {
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _ingredientsServiceMock = new Mock<IIngredientsService>();
        var catalog = new Mock<CgmLink.Nutrition.INutritionCatalog>();
        catalog.SetupGet(c => c.Source).Returns("external");
        _mealService = new MealService(_mealsRepositoryMock.Object, catalog.Object,
            _ingredientsServiceMock.Object, Mock.Of<IRepository<NutritionIngredient>>());

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    private MealIngredient CreateMealIngredient(Guid mealId, Guid ingredientId, IngredientServing serving, decimal quantity)
    {
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
        };

        return new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            IngredientId = ingredientId,
            Ingredient = ingredient,
            ServingId = serving.Id,
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
            _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.GetAll(It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<GetMealResponse>>());
        var okResult = result.Result as Ok<GetMealResponse>;
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
            _mealService, CancellationToken.None);

        var okResult = result.Result as Ok<GetMealResponse>;
        Assert.That(okResult!.Value.Ingredients.Count, Is.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(okResult.Value.Ingredients.First().IngredientId, Is.EqualTo(ingredientId1));
            Assert.That(okResult.Value.Ingredients.First().Calories, Is.EqualTo(200));
            Assert.That(okResult.Value.Ingredients.Last().IngredientId, Is.EqualTo(ingredientId2));
            Assert.That(okResult.Value.Ingredients.Last().Calories, Is.EqualTo(50));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task HandleAsync_Should_Return_Nutrition_Ingredients_From_Service(bool includePersonalIngredient)
    {
        var mealId = Guid.NewGuid();
        var meal = CreateMeal(mealId);
        if (includePersonalIngredient)
        {
            var ingredientId = Guid.NewGuid();
            meal.Ingredients.Add(CreateMealIngredient(mealId, ingredientId,
                CreateServing(ingredientId, 100m, 10m, 5m, 2m), 2m));
        }
        var identity = new NutritionIngredient { Source = "external", ProductId = "product-1" };
        foreach (var servingId in new[] { "cup", "spoon" })
        {
            var serving = new NutritionServing
            {
                NutritionIngredient = identity,
                NutritionIngredientId = identity.Id,
                ServingId = servingId,
            };
            meal.NutritionIngredients.Add(new MealNutritionIngredient
            {
                MealId = meal.Id,
                NutritionIngredientId = identity.Id,
                NutritionIngredient = identity,
                ServingId = serving.Id,
                Serving = serving,
                Quantity = servingId == "cup" ? 2.5m : 1m,
                Created = DateTimeOffset.UtcNow,
            });
        }
        SetupMeal(meal);
        var cachedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var product = IngredientResponse.FromProduct(new CgmLink.Nutrition.Source.NutritionProduct
        {
            ProductId = "product-1",
            Name = "External milk",
            Servings =
            [
                new CgmLink.Nutrition.Source.NutritionServing
                {
                    ExternalId = "cup", Description = "1 cup", ServingAmount = 1, ServingUnit = "cup",
                    Calories = 80, Carbs = 8, Protein = 4, Fat = 2,
                },
                new CgmLink.Nutrition.Source.NutritionServing
                {
                    ExternalId = "spoon", Description = "1 spoon", Calories = 10, Carbs = 1, Protein = 0.5m, Fat = 0.25m,
                },
            ],
        }, cachedAt, "Provider attribution");
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        _ingredientsServiceMock.Setup(s => s.GetIngredientAsync("product-1", _userId, token, IngredientType.External))
            .ReturnsAsync(product);

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealService, token);

        var response = ((Ok<GetMealResponse>)result.Result).Value;
        var cup = response.NutritionIngredients.First();
        var spoon = response.NutritionIngredients.Last();
        Assert.Multiple(() =>
        {
            Assert.That(response.Ingredients.Count, Is.EqualTo(includePersonalIngredient ? 1 : 0));
            Assert.That(response.NutritionIngredients.Count, Is.EqualTo(2));
            Assert.That(cup.ProductId, Is.EqualTo("product-1"));
            Assert.That(cup.IngredientName, Is.EqualTo("External milk"));
            Assert.That(cup.Quantity, Is.EqualTo(2.5m));
            Assert.That(cup.Serving.ServingId, Is.EqualTo("cup"));
            Assert.That(cup.Serving.Description, Is.EqualTo("1 cup"));
            Assert.That(cup.Serving.ServingAmount, Is.EqualTo(1));
            Assert.That(cup.Serving.ServingUnit, Is.EqualTo("cup"));
            Assert.That(cup.Calories, Is.EqualTo(200));
            Assert.That(cup.Carbs, Is.EqualTo(20));
            Assert.That(cup.Protein, Is.EqualTo(10));
            Assert.That(cup.Fat, Is.EqualTo(5));
            Assert.That(cup.CachedAt, Is.EqualTo(cachedAt));
            Assert.That(cup.Attribution, Is.EqualTo("Provider attribution"));
            Assert.That(spoon.Serving.ServingId, Is.EqualTo("spoon"));
            Assert.That(spoon.Calories, Is.EqualTo(10));
        });
        _ingredientsServiceMock.Verify(s => s.GetIngredientAsync("product-1", _userId, token, IngredientType.External), Times.Once);
        if (includePersonalIngredient)
        {
            Assert.That(response.Ingredients.Single().Calories, Is.EqualTo(200));
        }
    }

    [TestCase("product", "INGREDIENT_NOT_FOUND")]
    [TestCase("serving", "INGREDIENT_SERVING_ID_INVALID")]
    [TestCase("source", "INGREDIENT_ID_INVALID")]
    [TestCase("servingIdentity", "INGREDIENT_SERVING_ID_INVALID")]
    public void HandleAsync_Should_Reject_Unresolvable_Nutrition_Ingredients(string missing, string error)
    {
        var meal = CreateMeal(Guid.NewGuid());
        var identity = new NutritionIngredient
        {
            Source = missing == "source" ? "unsupported" : "external",
            ProductId = "product-1",
        };
        var serving = new NutritionServing
        {
            NutritionIngredient = identity,
            NutritionIngredientId = identity.Id,
            ServingId = "cup",
        };
        meal.NutritionIngredients.Add(new MealNutritionIngredient
        {
            MealId = meal.Id,
            NutritionIngredientId = identity.Id,
            NutritionIngredient = identity,
            ServingId = serving.Id,
            Serving = missing == "servingIdentity" ? null : serving,
            Quantity = 1,
            Created = DateTimeOffset.UtcNow,
        });
        SetupMeal(meal);
        var lookup = _ingredientsServiceMock.Setup(s => s.GetIngredientAsync(
            "product-1", _userId, CancellationToken.None, IngredientType.External));
        if (missing == "product")
        {
            lookup.ThrowsAsync(new NotFoundException("INGREDIENT_NOT_FOUND"));
        }
        else
        {
            lookup.ReturnsAsync(IngredientResponse.FromProduct(new CgmLink.Nutrition.Source.NutritionProduct
            {
                ProductId = "product-1",
                Name = "Milk",
                Servings = [],
            }, null, "Provider attribution"));
        }

        Assert.That(async () => await Endpoint.HandleAsync(meal.Id, _currentUserMock.Object,
            _mealService, CancellationToken.None),
            Throws.Exception.With.Message.EqualTo(error));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Empty_Ingredients_When_Meal_Has_No_Ingredients()
    {
        var mealId = Guid.NewGuid();
        SetupMeal(CreateMeal(mealId));

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealService, CancellationToken.None);

        var okResult = result.Result as Ok<GetMealResponse>;
        Assert.That(okResult!.Value.Ingredients, Is.Empty);
        Assert.That(okResult.Value.NutritionIngredients, Is.Empty);
        _ingredientsServiceMock.Verify(s => s.GetIngredientAsync(It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>(), It.IsAny<IngredientType>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_Should_Return_Zero_Nutrition_When_Serving_Not_Loaded()
    {
        var mealId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var mealIngredient = new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            IngredientId = ingredientId,
            Ingredient = new Ingredient
            {
                Id = ingredientId,
                Name = "Milk",
                Created = DateTimeOffset.UtcNow,
            },
            ServingId = Guid.NewGuid(),
            Serving = null,
            Quantity = 2m,
            Created = DateTimeOffset.UtcNow,
        };
        SetupMeal(CreateMeal(mealId, new List<MealIngredient> { mealIngredient }));

        var result = await Endpoint.HandleAsync(mealId, _currentUserMock.Object,
            _mealService, CancellationToken.None);

        var okResult = result.Result as Ok<GetMealResponse>;
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
                _mealService, CancellationToken.None),
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
                _mealService, CancellationToken.None),
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
                _mealService, CancellationToken.None),
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
                _mealService, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}
