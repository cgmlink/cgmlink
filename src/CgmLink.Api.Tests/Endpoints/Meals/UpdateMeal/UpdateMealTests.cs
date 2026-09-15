using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.UpdateMeal;
using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals;

[TestFixture]
public class UpdateMealTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<UpdateMealRequest>> _validatorMock;
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;
    private IIngredientsService _ingredientsService;
    private IMealService _mealService;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<UpdateMealRequest>>();
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _ingredientsService = new IngredientsService(_ingredientsRepositoryMock.Object);
        _mealService = new MealService(_mealsRepositoryMock.Object);

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateMealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mealsRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private Meal CreateMeal(Guid id)
    {
        return new Meal
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
    }

    private Ingredient CreateIngredient(Guid id)
    {
        return new Ingredient
        {
            Id = id,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = id, Created = DateTimeOffset.UtcNow } },
        };
    }

    private IngredientServing CreateServing(Guid id, Guid ingredientId)
    {
        return new IngredientServing
        {
            Id = id,
            IngredientId = ingredientId,
            Description = "1 cup",
            Calories = 100,
            Carbs = 10,
            Protein = 5,
            Fat = 2,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private void SetupMeal(Meal meal)
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));
    }

    private void SetupIngredients(IEnumerable<Ingredient> ingredients)
    {
        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(ingredients.ToList()));
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new UpdateMealRequest { Name = "" };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Name", "Name is required.") }
            });

        var result = await Endpoint.HandleAsync(Guid.NewGuid(), request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
            _mealService, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_When_Only_Name_Is_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Dinner" };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
            _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Name == "Dinner" &&
            m.Calories == 500 &&
            m.Updated != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.TypeOf<Ok<UpdateMealResponse>>());
            Assert.That(okResult!.Value.Name, Is.EqualTo("Dinner"));
            Assert.That(okResult.Value.IngredientCount, Is.EqualTo(0));
            Assert.That(okResult.Value.Updated, Is.Not.Null);
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_When_All_Fields_Are_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest
        {
            Name = "Lunch",
            ImageUrl = "https://example.com/new-image.jpg",
            ThumbnailUrl = "https://example.com/new-thumb.jpg",
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
            _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Name == "Lunch" &&
            m.ImageUrl == "https://example.com/new-image.jpg" &&
            m.ThumbnailUrl == "https://example.com/new-thumb.jpg"
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.That(okResult!.Value.Name, Is.EqualTo("Lunch"));
    }

    [Test]
    public async Task HandleAsync_Should_Replace_Ingredients_And_Recalculate_Nutrition()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        var ingredientId1 = Guid.NewGuid();
        var servingId1 = Guid.NewGuid();
        meal.Ingredients.Add(new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = id,
            IngredientId = ingredientId1,
            ServingId = servingId1,
            Quantity = 1,
            Created = DateTimeOffset.UtcNow,
        });
        SetupMeal(meal);

        var ingredientId2 = Guid.NewGuid();
        var servingId2 = Guid.NewGuid();
        var ingredient2 = CreateIngredient(ingredientId2);
        ingredient2.Servings.Add(CreateServing(servingId2, ingredientId2));
        SetupIngredients(new List<Ingredient> { ingredient2 });

        var request = new UpdateMealRequest
        {
            Name = "Updated",
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId2,
                    ServingId = servingId2,
                    Quantity = 2,
                }
            ]
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
            _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Name == "Updated" &&
            m.Ingredients.Count == 1 &&
            m.Ingredients.Single().IngredientId == ingredientId2 &&
            m.Ingredients.Single().Quantity == 2 &&
            m.Calories == 200
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.IngredientCount, Is.EqualTo(1));
            Assert.That(okResult.Value.Calories, Is.EqualTo(200));
            Assert.That(okResult.Value.Ingredients!.Single().IngredientId, Is.EqualTo(ingredientId2));
            Assert.That(okResult.Value.Ingredients.Single().Quantity, Is.EqualTo(2));
            Assert.That(okResult.Value.Ingredients.Single().IngredientName, Is.EqualTo("Milk"));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Update_Existing_Ingredient_In_Place()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        var ingredientId = Guid.NewGuid();
        var servingId1 = Guid.NewGuid();
        var servingId2 = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId1, ingredientId));
        ingredient.Servings.Add(CreateServing(servingId2, ingredientId));

        meal.Ingredients.Add(new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = id,
            IngredientId = ingredientId,
            ServingId = servingId1,
            Quantity = 1,
            Ingredient = ingredient,
            Serving = ingredient.Servings.Single(s => s.Id == servingId1),
            Created = DateTimeOffset.UtcNow,
        });
        SetupMeal(meal);
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = servingId2,
                    Quantity = 3,
                }
            ]
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
            _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Ingredients.Count == 1 &&
            m.Ingredients.Single().ServingId == servingId2 &&
            m.Ingredients.Single().Quantity == 3
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.That(okResult!.Value.Ingredients!.Single().ServingId, Is.EqualTo(servingId2));
        Assert.That(okResult.Value.Ingredients!.Single().Quantity, Is.EqualTo(3));
    }

    [Test]
    public async Task HandleAsync_Should_Not_Recalculate_Nutrition_When_Ingredients_Are_Not_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "New Name" };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
            _mealService, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.GetAll(), Times.Never);

        Assert.That(result.Result, Is.TypeOf<Ok<UpdateMealResponse>>());
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Found()
    {
        var id = Guid.NewGuid();

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        var request = new UpdateMealRequest { Name = "Updated" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
                _mealService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Linked_To_User()
    {
        var id = Guid.NewGuid();
        var meal = new Meal
        {
            Id = id,
            UserId = Guid.NewGuid(),
            Name = "Breakfast",
            Calories = 500,
            Carbs = 50,
            Protein = 25,
            Fat = 20,
            Created = DateTimeOffset.UtcNow,
        };
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Updated" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
                _mealService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Is_Soft_Deleted()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        meal.Deleted = DateTimeOffset.UtcNow;
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Updated" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
                _mealService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Ingredient_Not_Found()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1,
                }
            ]
        };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
                _mealService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Serving_Not_Found_On_Ingredient()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var ingredientId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(Guid.NewGuid(), ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1,
                }
            ]
        };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
                _mealService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        var request = new UpdateMealRequest { Name = "Updated" };

        Assert.That(async () => await Endpoint.HandleAsync(Guid.NewGuid(), request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsService,
                _mealService, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}