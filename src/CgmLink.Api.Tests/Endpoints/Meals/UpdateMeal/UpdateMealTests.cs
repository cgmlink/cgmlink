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
using CgmLink.Resources;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals.UpdateMeal;

[TestFixture]
public class UpdateMealTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<UpdateMealRequest>> _validatorMock;
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IMealService> _mealServiceMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<UpdateMealRequest>>();
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _mealServiceMock = new Mock<IMealService>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateMealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mealServiceMock
            .Setup(s => s.RecalculateNutrition(It.IsAny<Meal>()))
            .Returns((Meal meal) => meal);

        _mealsRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));
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
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Created = DateTimeOffset.UtcNow.AddDays(-1),
        };
    }

    private void SetupMeal(Meal meal)
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal> { meal }));
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
            Calories = 100m,
            Carbs = 10m,
            Protein = 5m,
            Fat = 2m,
            Created = DateTimeOffset.UtcNow,
        };
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
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
            _mealServiceMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_When_Only_Name_Is_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Updated Breakfast" };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
            _mealServiceMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Name == "Updated Breakfast" &&
            m.ImageUrl == "https://example.com/image.jpg" &&
            m.ThumbnailUrl == "https://example.com/thumb.jpg" &&
            m.Updated != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult, Is.TypeOf<Ok<UpdateMealResponse>>());
            Assert.That(okResult!.Value.Name, Is.EqualTo("Updated Breakfast"));
            Assert.That(okResult.Value.ImageUrl, Is.EqualTo("https://example.com/image.jpg"));
            Assert.That(okResult.Value.ThumbnailUrl, Is.EqualTo("https://example.com/thumb.jpg"));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_And_Not_Change_Any_Values_When_Request_Is_Empty()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest();

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
            _mealServiceMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Name == "Breakfast" &&
            m.ImageUrl == "https://example.com/image.jpg" &&
            m.ThumbnailUrl == "https://example.com/thumb.jpg" &&
            m.Updated != null
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.That(okResult!.Value.Name, Is.EqualTo("Breakfast"));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_When_All_Properties_Are_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest
        {
            Name = "Dinner",
            ImageUrl = "https://example.com/new-image.jpg",
            ThumbnailUrl = "https://example.com/new-thumb.jpg",
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
            _mealServiceMock.Object, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Meal>(m =>
            m.Name == "Dinner" &&
            m.ImageUrl == "https://example.com/new-image.jpg" &&
            m.ThumbnailUrl == "https://example.com/new-thumb.jpg"
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.That(okResult!.Value.Name, Is.EqualTo("Dinner"));
    }

    [Test]
    public async Task HandleAsync_Should_Replace_Ingredients_When_Ingredients_Are_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        meal.Ingredients.Add(new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = id,
            IngredientId = Guid.NewGuid(),
            ServingId = Guid.NewGuid(),
            Quantity = 1m,
            Created = DateTimeOffset.UtcNow,
        });
        SetupMeal(meal);

        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId, ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = servingId,
                    Quantity = 2m,
                }
            ]
        };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
            _mealServiceMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Ingredients, Has.Count.EqualTo(1));
            Assert.That(meal.Ingredients.First().IngredientId, Is.EqualTo(ingredientId));
            Assert.That(meal.Ingredients.First().ServingId, Is.EqualTo(servingId));
            Assert.That(meal.Ingredients.First().Quantity, Is.EqualTo(2m));
        });

        _mealServiceMock.Verify(s => s.RecalculateNutrition(It.Is<Meal>(m =>
            m.Ingredients.Count == 1
        )), Times.Once);

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.That(okResult!.Value.IngredientCount, Is.EqualTo(1));
    }

    [Test]
    public async Task HandleAsync_Should_Not_Change_Ingredients_When_Ingredients_Are_Not_Provided()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        var existing = new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = id,
            IngredientId = Guid.NewGuid(),
            ServingId = Guid.NewGuid(),
            Quantity = 1m,
            Created = DateTimeOffset.UtcNow,
        };
        meal.Ingredients.Add(existing);
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Brunch" };

        var result = await Endpoint.HandleAsync(id, request, _validatorMock.Object,
            _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
            _mealServiceMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Ingredients, Has.Count.EqualTo(1));
            Assert.That(meal.Ingredients.First(), Is.SameAs(existing));
        });

        var okResult = result.Result as Ok<UpdateMealResponse>;
        Assert.That(okResult!.Value.IngredientCount, Is.EqualTo(1));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Not_Found()
    {
        var id = Guid.NewGuid();

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        var request = new UpdateMealRequest { Name = "Updated Breakfast" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Is_Soft_Deleted()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        meal.Deleted = DateTimeOffset.UtcNow;
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Updated Breakfast" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Is_Not_Owned_By_User()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        meal.UserId = Guid.NewGuid();
        SetupMeal(meal);

        var request = new UpdateMealRequest { Name = "Updated Breakfast" };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Ingredient_Not_Found()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                }
            ]
        };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo(ValidationMessages.IngredientIdInvalid));

        _mealsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Ingredient_Not_Linked_To_User()
    {
        var id = Guid.NewGuid();
        var meal = CreateMeal(id);
        SetupMeal(meal);

        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = Guid.NewGuid(), IngredientId = ingredientId, Created = DateTimeOffset.UtcNow } },
        };
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new UpdateMealRequest
        {
            Ingredients =
            [
                new UpdateMealRequest.UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                }
            ]
        };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo(ValidationMessages.IngredientIdInvalid));
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
                    Quantity = 1m,
                }
            ]
        };

        Assert.That(async () => await Endpoint.HandleAsync(id, request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo(ValidationMessages.IngredientIdInvalid));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        var request = new UpdateMealRequest { Name = "Updated Breakfast" };

        Assert.That(async () => await Endpoint.HandleAsync(Guid.NewGuid(), request, _validatorMock.Object,
                _currentUserMock.Object, _mealsRepositoryMock.Object, _ingredientsRepositoryMock.Object,
                _mealServiceMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}