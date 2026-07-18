using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using CgmLink.Api.Endpoints.Meals.UpdateMeal;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Meals;

[TestFixture]
public class UpdateMealTests
{
    private Mock<IValidator<UpdateMealRequest>> _validatorMock;
    private Mock<IRepository<Meal>> _mealRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientRepositoryMock;
    private Mock<IRepository<MealIngredient>> _mealIngredientRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<UpdateMealRequest>>();
        _mealRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientRepositoryMock = new Mock<IRepository<Ingredient>>();
        _mealIngredientRepositoryMock = new Mock<IRepository<MealIngredient>>();
        _currentUserMock = new Mock<ICurrentUser>();
    }

    [Test]
    public async Task HandleAsync_ReturnsValidationProblem_WhenRequestIsInvalid()
    {
        var request = new UpdateMealRequest
        {
            Id = Guid.NewGuid(),
            Name = "",
            MealIngredients = new List<UpdateMealIngredientRequest>()
        };

        var validationResult = new ValidationResult([
            new ValidationFailure("Name", "Name is required.")
        ]);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var result = await Endpoint.HandleAsync(
            request,
            _validatorMock.Object,
            _currentUserMock.Object,
            _mealRepositoryMock.Object,
            _ingredientRepositoryMock.Object,
            _mealIngredientRepositoryMock.Object,
            CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public async Task HandleAsync_Should_Return_NoContent_When_Update_Is_Successful()
    {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var ingredientId1 = Guid.NewGuid();
        var ingredientId2 = Guid.NewGuid();

        var request = new UpdateMealRequest
        {
            Id = mealId,
            Name = "Updated Meal",
            MealIngredients = new List<UpdateMealIngredientRequest>
            {
                new UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId1,
                    Quantity = 3
                },
                new UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId2,
                    Quantity = 1
                }
            }
        };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);

        var meal = new Meal
        {
            Id = mealId,
            UserId = userId,
            Name = "Original Meal",
            MealIngredients = new List<MealIngredient>(),
            Created = DateTimeOffset.UtcNow,
        };

        _mealRepositoryMock
            .Setup(m => m.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new[] { meal }.AsQueryable());

        _ingredientRepositoryMock
            .Setup(i => i.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var result = await Endpoint.HandleAsync(
            request,
            _validatorMock.Object,
            _currentUserMock.Object,
            _mealRepositoryMock.Object,
            _ingredientRepositoryMock.Object,
            _mealIngredientRepositoryMock.Object,
            CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NoContent>());
        _mealIngredientRepositoryMock.Verify(m => m.DeleteManyAsync(It.IsAny<Expression<Func<MealIngredient, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mealIngredientRepositoryMock.Verify(m => m.AddManyAsync(It.IsAny<IEnumerable<MealIngredient>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mealRepositoryMock.Verify(m => m.UpdateAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(meal.Name, Is.EqualTo("Updated Meal"));
        Assert.That(meal.Updated, Is.Not.Null);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Meal_Is_Not_Found()
    {
        var userId = Guid.NewGuid();
        var request = new UpdateMealRequest
        {
            Id = Guid.NewGuid(),
            Name = "Updated Meal",
            MealIngredients = new List<UpdateMealIngredientRequest>()
        };

        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _mealRepositoryMock
            .Setup(m => m.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(Array.Empty<Meal>().AsQueryable());

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, _mealIngredientRepositoryMock.Object, CancellationToken.None),
            Throws.TypeOf<NotFoundException>().With.Message.EqualTo("MEAL_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Is_Not_Found()
    {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();

        var request = new UpdateMealRequest
        {
            Id = mealId,
            Name = "Updated Meal",
            MealIngredients = new List<UpdateMealIngredientRequest>
            {
                new UpdateMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    Quantity = 1,
                }
            }
        };

        _currentUserMock.Setup(c => c.GetUserId()).Returns(userId);
        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var meal = new Meal
        {
            Id = mealId,
            UserId = userId,
            Name = "Original Meal",
            MealIngredients = new List<MealIngredient>(),
            Created = DateTimeOffset.UtcNow,
        };

        _mealRepositoryMock
            .Setup(m => m.Find(It.IsAny<Expression<Func<Meal, bool>>>(), It.IsAny<FindOptions>()))
            .Returns(new[] { meal }.AsQueryable());

        _ingredientRepositoryMock
            .Setup(i => i.CountAsync(It.IsAny<Expression<Func<Ingredient, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _mealRepositoryMock.Object, _ingredientRepositoryMock.Object, _mealIngredientRepositoryMock.Object, CancellationToken.None),
            Throws.TypeOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }
}
