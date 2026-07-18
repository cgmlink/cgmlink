using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using CgmLink.Api.Endpoints.Food.List;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;
using SortDirection = CgmLink.Api.Models.SortDirection;

namespace CgmLink.Api.Tests.Endpoints.Food;

[TestFixture]
public class ListFoodTests
{
    private static readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<ListFoodRequest>> _validatorMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IRepository<Meal>> _mealRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<ListFoodRequest>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _mealRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientRepositoryMock = new Mock<IRepository<Ingredient>>();
    }

    [Test]
    public void HandleAsync_Returns_Unauthorized_When_User_Is_Not_Authenticated()
    {
        _currentUserMock.Setup(x => x.GetUserId()).Throws(new UnauthorizedException("USER_NOT_LOGGED_IN", UnauthorizedSource.CgmLink));

        var request = new ListFoodRequest { Page = 0, PageSize = 10 };

        Assert.That(() => Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedException>().With.Message.EqualTo("USER_NOT_LOGGED_IN"));
    }

    [Test]
    public async Task HandleAsync_Returns_Validation_Problem_When_Request_Is_Invalid()
    {
        var request = new ListFoodRequest { Page = 0, PageSize = 10 };
        var validationResult = new ValidationResult(new List<ValidationFailure>
        {
            new ValidationFailure("Page", "Page is required")
        });

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(validationResult);

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<ValidationProblem>());
    }

    [Test]
    public async Task HandleAsync_Returns_Ok_With_Empty_Food_List()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Page = 0, PageSize = 10 };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_Returns_Ok_With_Meals_And_Ingredients()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Page = 0, PageSize = 10 };

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            Created = DateTimeOffset.UtcNow,
            Name = "TestIngredient",
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Calories = 100,
            Uom = UnitOfMeasurement.Grams
        };

        var mealId = Guid.NewGuid();
        var meals = new List<Meal>
        {
            new Meal
            {
                Id = mealId,
                UserId = userId,
                Name = "TestMeal",
                Created = DateTimeOffset.UtcNow,
                MealIngredients = new List<MealIngredient>
                {
                    new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient, Quantity = 2, IngredientId = ingredient.Id, MealId = mealId }
                }
            }
        };

        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), UserId = userId, Name = "Ingredient1", Created = DateTimeOffset.UtcNow, Uom = UnitOfMeasurement.Grams }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(ingredients));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(2));

            var mealResponse = okResult.Value.Food.First(f => f.FoodType == FoodType.Meal);
            Assert.That(mealResponse.Name, Is.EqualTo("TestMeal"));
            Assert.That(mealResponse.TotalCalories, Is.EqualTo(200));
            Assert.That(mealResponse.TotalCarbs, Is.EqualTo(20));
            Assert.That(mealResponse.TotalProtein, Is.EqualTo(10));
            Assert.That(mealResponse.TotalFat, Is.EqualTo(4));
            Assert.That(mealResponse.Ingredients, Has.Count.EqualTo(1));

            var ingredientResponse = okResult.Value.Food.First(f => f.FoodType == FoodType.Ingredient);
            Assert.That(ingredientResponse.Name, Is.EqualTo("Ingredient1"));
            Assert.That(ingredientResponse.FoodType, Is.EqualTo(FoodType.Ingredient));
        });
    }

    [Test]
    public async Task HandleAsync_Filters_Meals_And_Ingredients_When_Search_Is_Provided()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Search = "apple", Page = 0, PageSize = 10 };

        var meals = new List<Meal>
        {
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Apple Pie", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() },
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Banana Bread", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() }
        }.AsQueryable();

        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), UserId = userId, Name = "Apple Sauce", Created = DateTimeOffset.UtcNow, Uom = UnitOfMeasurement.Grams },
            new Ingredient { Id = Guid.NewGuid(), UserId = userId, Name = "Orange Juice", Created = DateTimeOffset.UtcNow, Uom = UnitOfMeasurement.Grams }
        }.AsQueryable();

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns((Expression<Func<Meal, bool>> predicate, FindOptions _) => new TestAsyncEnumerable<Meal>(meals.Where(predicate)));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns((Expression<Func<Ingredient, bool>> predicate, FindOptions _) => new TestAsyncEnumerable<Ingredient>(ingredients.Where(predicate)));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(2));
            Assert.That(okResult.Value.Food.All(f => f.Name.Contains("apple", StringComparison.OrdinalIgnoreCase)), Is.True);
        });
    }

    [Test]
    public async Task HandleAsync_Sorts_By_Name_Ascending()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Sort = "Name", SortDirection = SortDirection.Asc, Page = 0, PageSize = 10 };

        var meals = new List<Meal>
        {
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Zebra Meal", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() },
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Apple Meal", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food.First().Name, Is.EqualTo("Apple Meal"));
            Assert.That(okResult.Value.Food.Last().Name, Is.EqualTo("Zebra Meal"));
        });
    }

    [Test]
    public async Task HandleAsync_Sorts_By_Name_Descending()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Sort = "Name", SortDirection = SortDirection.Desc, Page = 0, PageSize = 10 };

        var meals = new List<Meal>
        {
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Apple Meal", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() },
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Zebra Meal", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food.First().Name, Is.EqualTo("Zebra Meal"));
            Assert.That(okResult.Value.Food.Last().Name, Is.EqualTo("Apple Meal"));
        });
    }

    [Test]
    public async Task HandleAsync_Sorts_By_TotalCalories_Descending_By_Default()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Sort = "TotalCalories", Page = 0, PageSize = 10 };

        var ingredient1 = new Ingredient { Id = Guid.NewGuid(), Name = "HighCal", Created = DateTimeOffset.UtcNow, Calories = 100, Carbs = 10, Protein = 5, Fat = 2, Uom = UnitOfMeasurement.Grams };
        var ingredient2 = new Ingredient { Id = Guid.NewGuid(), Name = "LowCal", Created = DateTimeOffset.UtcNow, Calories = 50, Carbs = 5, Protein = 2, Fat = 1, Uom = UnitOfMeasurement.Grams };

        var mealId1 = Guid.NewGuid();
        var mealId2 = Guid.NewGuid();
        var meals = new List<Meal>
        {
            new Meal { Id = mealId1, UserId = userId, Name = "Low Cal", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient> { new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient2, Quantity = 1, IngredientId = ingredient2.Id, MealId = mealId1 } } },
            new Meal { Id = mealId2, UserId = userId, Name = "High Cal", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient> { new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient1, Quantity = 1, IngredientId = ingredient1.Id, MealId = mealId2 } } }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food.First().Name, Is.EqualTo("High Cal"));
            Assert.That(okResult.Value.Food.Last().Name, Is.EqualTo("Low Cal"));
        });
    }

    [Test]
    public async Task HandleAsync_Sorts_By_Multiple_Fields()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Sort = "Name,TotalCalories", SortDirection = SortDirection.Asc, Page = 0, PageSize = 10 };

        var ingredient = new Ingredient { Id = Guid.NewGuid(), Name = "TestIngredient", Created = DateTimeOffset.UtcNow, Calories = 100, Carbs = 10, Protein = 5, Fat = 2, Uom = UnitOfMeasurement.Grams };

        var mealId1 = Guid.NewGuid();
        var mealId2 = Guid.NewGuid();
        var meals = new List<Meal>
        {
            new Meal { Id = mealId1, UserId = userId, Name = "Apple", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient> { new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient, Quantity = 2, IngredientId = ingredient.Id, MealId = mealId1 } } },
            new Meal { Id = mealId2, UserId = userId, Name = "Apple", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient> { new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient, Quantity = 1, IngredientId = ingredient.Id, MealId = mealId2 } } }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food.First().TotalCalories, Is.EqualTo(100));
            Assert.That(okResult.Value.Food.Last().TotalCalories, Is.EqualTo(200));
        });
    }

    [Test]
    public async Task HandleAsync_Paginates_Results()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Page = 1, PageSize = 2 };

        var meals = new List<Meal>
        {
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Meal1", Created = DateTimeOffset.UtcNow.AddDays(-2), MealIngredients = new List<MealIngredient>() },
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Meal2", Created = DateTimeOffset.UtcNow.AddDays(-1), MealIngredients = new List<MealIngredient>() },
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Meal3", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.Food.First().Name, Is.EqualTo("Meal1"));
            Assert.That(okResult.Value.NumberOfPages, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Handle_Null_Ingredient_In_Meal()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { Page = 0, PageSize = 10 };

        var mealId = Guid.NewGuid();
        var meals = new List<Meal>
        {
            new Meal
            {
                Id = mealId,
                UserId = userId,
                Name = "Meal With Null Ingredient",
                Created = DateTimeOffset.UtcNow,
                MealIngredients = new List<MealIngredient>
                {
                    new MealIngredient { Id = Guid.NewGuid(), Quantity = 2, Ingredient = null, IngredientId = Guid.NewGuid(), MealId = mealId }
                }
            }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food.First().TotalCalories, Is.EqualTo(0));
            Assert.That(okResult.Value.Food.First().TotalCarbs, Is.EqualTo(0));
            Assert.That(okResult.Value.Food.First().TotalProtein, Is.EqualTo(0));
            Assert.That(okResult.Value.Food.First().TotalFat, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task HandleAsync_Returns_Only_Meals_When_FoodType_Is_Meal()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { FoodType = FoodType.Meal, Page = 0, PageSize = 10 };

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "TestIngredient",
            Created = DateTimeOffset.UtcNow,
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Calories = 100,
            Uom = UnitOfMeasurement.Grams
        };

        var mealId = Guid.NewGuid();
        var meals = new List<Meal>
        {
            new Meal
            {
                Id = mealId,
                UserId = userId,
                Name = "TestMeal",
                Created = DateTimeOffset.UtcNow,
                MealIngredients = new List<MealIngredient>
                {
                    new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient, Quantity = 2, IngredientId = ingredient.Id, MealId = mealId }
                }
            }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.Food.First().FoodType, Is.EqualTo(FoodType.Meal));
            Assert.That(okResult.Value.Food.First().Name, Is.EqualTo("TestMeal"));
        });

        _ingredientRepositoryMock.Verify(
            r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_Returns_Only_Ingredients_When_FoodType_Is_Ingredient()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { FoodType = FoodType.Ingredient, Page = 0, PageSize = 10 };

        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), UserId = userId, Name = "Apple", Created = DateTimeOffset.UtcNow, Calories = 52, Carbs = 14, Protein = 0, Fat = 0, Uom = UnitOfMeasurement.Grams },
            new Ingredient { Id = Guid.NewGuid(), UserId = userId, Name = "Banana", Created = DateTimeOffset.UtcNow, Calories = 89, Carbs = 23, Protein = 1, Fat = 0, Uom = UnitOfMeasurement.Grams }
        };

        var meals = new List<Meal>
        {
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Fruit Salad", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(ingredients));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(2));
            Assert.That(okResult.Value.Food.All(f => f.FoodType == FoodType.Ingredient), Is.True);
        });

        _mealRepositoryMock.Verify(
            r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()),
            Times.Never);
    }

    [Test]
    public async Task HandleAsync_Returns_Both_When_FoodType_Is_Null()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { FoodType = null, Page = 0, PageSize = 10 };

        var ingredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "TestIngredient",
            Created = DateTimeOffset.UtcNow,
            Uom = UnitOfMeasurement.Grams
        };

        var mealId = Guid.NewGuid();
        var meals = new List<Meal>
        {
            new Meal
            {
                Id = mealId,
                UserId = userId,
                Name = "TestMeal",
                Created = DateTimeOffset.UtcNow,
                MealIngredients = new List<MealIngredient>
                {
                    new MealIngredient { Id = Guid.NewGuid(), Ingredient = ingredient, Quantity = 1, IngredientId = ingredient.Id, MealId = mealId }
                }
            }
        };

        var ingredients = new List<Ingredient>
        {
            new Ingredient { Id = Guid.NewGuid(), UserId = userId, Name = "StandaloneIngredient", Created = DateTimeOffset.UtcNow, Uom = UnitOfMeasurement.Grams }
        };

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Meal>(meals));
        _ingredientRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(ingredients));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(2));
            Assert.That(okResult.Value.Food.Any(f => f.FoodType == FoodType.Meal), Is.True);
            Assert.That(okResult.Value.Food.Any(f => f.FoodType == FoodType.Ingredient), Is.True);
        });

        _mealRepositoryMock.Verify(
            r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()),
            Times.Once);
        _ingredientRepositoryMock.Verify(
            r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()),
            Times.Once);
    }

    [Test]
    public async Task HandleAsync_FoodType_Meal_With_Search_Filters_Correctly()
    {
        var userId = Guid.NewGuid();
        var request = new ListFoodRequest { FoodType = FoodType.Meal, Search = "apple", Page = 0, PageSize = 10 };

        var meals = new List<Meal>
        {
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Apple Pie", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() },
            new Meal { Id = Guid.NewGuid(), UserId = userId, Name = "Banana Bread", Created = DateTimeOffset.UtcNow, MealIngredients = new List<MealIngredient>() }
        }.AsQueryable();

        _validatorMock.Setup(v => v.ValidateAsync(request, default)).ReturnsAsync(new ValidationResult());
        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _mealRepositoryMock.Setup(r => r.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns((Expression<Func<Meal, bool>> predicate, FindOptions _) => new TestAsyncEnumerable<Meal>(meals.Where(predicate)));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<ListFoodResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.InstanceOf<Ok<ListFoodResponse>>());
            Assert.That(okResult.Value.Food, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.Food.First().Name, Is.EqualTo("Apple Pie"));
            Assert.That(okResult.Value.Food.First().FoodType, Is.EqualTo(FoodType.Meal));
        });

        _ingredientRepositoryMock.Verify(
            r => r.Find(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>()),
            Times.Never);
    }
}
