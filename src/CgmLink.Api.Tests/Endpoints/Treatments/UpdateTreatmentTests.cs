using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Treatments.UpdateTreatment;
using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Treatments;

[TestFixture]
public class UpdateTreatmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<UpdateTreatmentRequest>> _validatorMock;
    private Mock<IRepository<Treatment>> _treatmentsRepositoryMock;
    private Mock<IRepository<Reading>> _readingsRepositoryMock;
    private Mock<IRepository<Insulin>> _insulinsRepositoryMock;
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;
    private IMealService _mealService;
    private IIngredientsService _ingredientsService;
    private ITreatmentService _treatmentService;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<UpdateTreatmentRequest>>();
        _treatmentsRepositoryMock = new Mock<IRepository<Treatment>>();
        _readingsRepositoryMock = new Mock<IRepository<Reading>>();
        _insulinsRepositoryMock = new Mock<IRepository<Insulin>>();
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _mealService = new MealService(_mealsRepositoryMock.Object);
        _ingredientsService = new IngredientsService(_ingredientsRepositoryMock.Object);
        _treatmentService = new TreatmentService();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTreatmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _treatmentsRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _treatmentsRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Insulin?)null);

        _readingsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Reading, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reading?)null);

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));
    }

    private Insulin CreateInsulin(Guid id)
    {
        return new Insulin
        {
            Id = id,
            UserId = _userId,
            Name = "Fiasp",
            Type = InsulinType.Bolus,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private Reading CreateReading(Guid id)
    {
        return new Reading
        {
            Id = id,
            UserId = _userId,
            Created = DateTimeOffset.UtcNow,
            GlucoseLevel = 5.0,
            Direction = ReadingDirection.Steady,
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

    private Meal CreateMeal(Guid id)
    {
        return new Meal
        {
            Id = id,
            UserId = _userId,
            Name = "Breakfast",
            Calories = 100m,
            Carbs = 10m,
            Protein = 5m,
            Fat = 2m,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private void SetupTreatment(Treatment treatment)
    {
        _treatmentsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Treatment>(new List<Treatment> { treatment }));
    }

    private void SetupMeals(IEnumerable<Meal> meals)
    {
        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(meals.ToList()));
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
        var treatmentId = Guid.NewGuid();

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateTreatmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Meals", "At least one is required.") }
            });

        var result = await Endpoint.HandleAsync(treatmentId, new UpdateTreatmentRequest(),
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Not_Found()
    {
        var treatmentId = Guid.NewGuid();

        _treatmentsRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest { InsulinId = Guid.NewGuid(), Units = 5m }
        };

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, request,
                _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
                _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
                _mealService, _ingredientsService, _treatmentService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Insulin_Not_Found()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        SetupTreatment(treatment);

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Insulin?)null);

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest { InsulinId = Guid.NewGuid(), Units = 5m }
        };

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, request,
                _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
                _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
                _mealService, _ingredientsService, _treatmentService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INSULIN_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Reading_Not_Found()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        SetupTreatment(treatment);

        _readingsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Reading, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reading?)null);

        var request = new UpdateTreatmentRequest { ReadingId = Guid.NewGuid() };

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, request,
                _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
                _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
                _mealService, _ingredientsService, _treatmentService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("READING_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest { InsulinId = Guid.NewGuid(), Units = 5m }
        };

        Assert.That(async () => await Endpoint.HandleAsync(Guid.NewGuid(), request,
                _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
                _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
                _mealService, _ingredientsService, _treatmentService, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }

    [Test]
    public async Task HandleAsync_Should_Update_Existing_Injection()
    {
        var treatmentId = Guid.NewGuid();
        var insulinId = Guid.NewGuid();
        var existingInsulin = CreateInsulin(insulinId);
        var treatment = CreateTreatment(treatmentId);
        treatment.Injection!.Insulin = existingInsulin;
        SetupTreatment(treatment);

        var newInsulinId = Guid.NewGuid();
        var newInsulin = CreateInsulin(newInsulinId);
        newInsulin.Name = "Novorapid";
        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newInsulin);

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest { InsulinId = newInsulinId, Units = 8m }
        };

        var result = await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Treatment>(t =>
            t.InjectionId == treatment.InjectionId &&
            t.Injection!.InsulinId == newInsulinId &&
            t.Injection.Units == 8m
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.InsulinName, Is.EqualTo("Novorapid"));
            Assert.That(okResult.Value.InsulinUnits, Is.EqualTo(8m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Add_Injection_When_Treatment_Has_None()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.Injection = null;
        treatment.InjectionId = null;
        SetupTreatment(treatment);

        var insulinId = Guid.NewGuid();
        var insulin = CreateInsulin(insulinId);
        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(insulin);

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest { InsulinId = insulinId, Units = 10m }
        };

        var result = await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Treatment>(t =>
            t.InjectionId != null &&
            t.Injection!.InsulinId == insulinId &&
            t.Injection.Units == 10m
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.InsulinName, Is.EqualTo("Fiasp"));
            Assert.That(okResult.Value.InsulinUnits, Is.EqualTo(10m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Update_Reading()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.ReadingId = null;
        treatment.Reading = null;
        SetupTreatment(treatment);

        var readingId = Guid.NewGuid();
        var reading = CreateReading(readingId);
        _readingsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Reading, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reading);

        var request = new UpdateTreatmentRequest { ReadingId = readingId };

        var result = await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Treatment>(t =>
            t.ReadingId == readingId
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateTreatmentResponse>;
        Assert.That(okResult!.Value, Is.Not.Null);
    }

    [Test]
    public async Task HandleAsync_Should_Update_Meals_And_Recalculate_Nutrition()
    {
        var treatmentId = Guid.NewGuid();
        var mealId1 = Guid.NewGuid();
        var meal1 = CreateMeal(mealId1);
        var treatment = CreateTreatment(treatmentId);
        treatment.Meals.Add(new TreatmentMeal
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            MealId = mealId1,
            Meal = meal1,
            Quantity = 1m,
            Created = DateTimeOffset.UtcNow,
        });
        treatment.Calories = meal1.Calories;
        treatment.Carbs = meal1.Carbs;
        treatment.Protein = meal1.Protein;
        treatment.Fat = meal1.Fat;
        SetupTreatment(treatment);

        var mealId2 = Guid.NewGuid();
        var meal2 = CreateMeal(mealId2);
        meal2.Calories = 200m;
        meal2.Carbs = 20m;
        meal2.Protein = 10m;
        meal2.Fat = 4m;
        SetupMeals(new List<Meal> { meal2 });

        var request = new UpdateTreatmentRequest
        {
            Meals =
            [
                new UpdateTreatmentRequest.UpdateTreatmentMealRequest { MealId = mealId1, Quantity = 2m },
                new UpdateTreatmentRequest.UpdateTreatmentMealRequest { MealId = mealId2, Quantity = 1m },
            ]
        };

        var result = await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Treatment>(t =>
            t.Meals.Count == 2 &&
            t.Meals.Any(tm => tm.MealId == mealId1 && tm.Quantity == 2m) &&
            t.Meals.Any(tm => tm.MealId == mealId2 && tm.Quantity == 1m) &&
            t.Calories == 400m &&
            t.Carbs == 40m &&
            t.Protein == 20m &&
            t.Fat == 8m
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.MealCount, Is.EqualTo(2));
            Assert.That(okResult.Value.IngredientCount, Is.EqualTo(0));
            Assert.That(okResult.Value.Calories, Is.EqualTo(400m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Update_Ingredients_And_Recalculate_Nutrition()
    {
        var treatmentId = Guid.NewGuid();
        var ingredientId1 = Guid.NewGuid();
        var servingId1 = Guid.NewGuid();
        var ingredient1 = CreateIngredient(ingredientId1);
        var serving1 = CreateServing(servingId1, ingredientId1);
        ingredient1.Servings.Add(serving1);

        var treatment = CreateTreatment(treatmentId);
        treatment.Ingredients.Add(new TreatmentIngredient
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            IngredientId = ingredientId1,
            Ingredient = ingredient1,
            ServingId = servingId1,
            Serving = serving1,
            Quantity = 1m,
            Created = DateTimeOffset.UtcNow,
        });
        treatment.Calories = serving1.Calories;
        treatment.Carbs = serving1.Carbs;
        treatment.Protein = serving1.Protein;
        treatment.Fat = serving1.Fat;
        SetupTreatment(treatment);

        var ingredientId2 = Guid.NewGuid();
        var servingId2 = Guid.NewGuid();
        var ingredient2 = CreateIngredient(ingredientId2);
        ingredient2.Servings.Add(CreateServing(servingId2, ingredientId2));
        SetupIngredients(new List<Ingredient> { ingredient2 });

        var request = new UpdateTreatmentRequest
        {
            Ingredients =
            [
                new UpdateTreatmentRequest.UpdateTreatmentIngredientRequest
                {
                    IngredientId = ingredientId1,
                    ServingId = servingId1,
                    Quantity = 2m,
                },
                new UpdateTreatmentRequest.UpdateTreatmentIngredientRequest
                {
                    IngredientId = ingredientId2,
                    ServingId = servingId2,
                    Quantity = 1m,
                },
            ]
        };

        var result = await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Treatment>(t =>
            t.Ingredients.Count == 2 &&
            t.Ingredients.Any(ti => ti.IngredientId == ingredientId1 && ti.Quantity == 2m) &&
            t.Ingredients.Any(ti => ti.IngredientId == ingredientId2 && ti.Quantity == 1m) &&
            t.Calories == 300m &&
            t.Carbs == 30m &&
            t.Protein == 15m &&
            t.Fat == 6m
        ), It.IsAny<CancellationToken>()), Times.Once);

        var okResult = result.Result as Ok<UpdateTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.MealCount, Is.EqualTo(0));
            Assert.That(okResult.Value.IngredientCount, Is.EqualTo(2));
            Assert.That(okResult.Value.Calories, Is.EqualTo(300m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Not_Call_UpdateTreatmentFoods_When_No_Food_Changes()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        SetupTreatment(treatment);

        var insulinId = Guid.NewGuid();
        var insulin = CreateInsulin(insulinId);
        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(insulin);

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest { InsulinId = insulinId, Units = 3m }
        };

        var result = await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.GetAll(), Times.Never);
        _ingredientsRepositoryMock.Verify(r => r.GetAll(), Times.Never);
        Assert.That(result.Result, Is.TypeOf<Ok<UpdateTreatmentResponse>>());
    }

    [Test]
    public async Task HandleAsync_Should_Set_Updated_Timestamp()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        SetupTreatment(treatment);

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(treatment.Injection!.Insulin);

        var request = new UpdateTreatmentRequest
        {
            Injection = new UpdateTreatmentRequest.UpdateInjectionRequest
            {
                InsulinId = treatment.Injection!.InsulinId,
                Units = treatment.Injection.Units
            }
        };

        var beforeUpdate = DateTimeOffset.UtcNow;

        await Endpoint.HandleAsync(treatmentId, request,
            _validatorMock.Object, _currentUserMock.Object, _treatmentsRepositoryMock.Object,
            _readingsRepositoryMock.Object, _insulinsRepositoryMock.Object,
            _mealService, _ingredientsService, _treatmentService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Treatment>(t =>
            t.Updated >= beforeUpdate && t.Updated <= DateTimeOffset.UtcNow
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    private Treatment CreateTreatment(Guid id)
    {
        var readingId = Guid.NewGuid();
        var insulinId = Guid.NewGuid();
        var injectionId = Guid.NewGuid();

        return new Treatment
        {
            Id = id,
            UserId = _userId,
            ReadingId = readingId,
            Reading = CreateReading(readingId),
            InjectionId = injectionId,
            Injection = new Injection
            {
                Id = injectionId,
                UserId = _userId,
                InsulinId = insulinId,
                Insulin = CreateInsulin(insulinId),
                Units = 5m,
                Created = DateTimeOffset.UtcNow,
            },
            Calories = 0m,
            Carbs = 0m,
            Protein = 0m,
            Fat = 0m,
            Created = DateTimeOffset.UtcNow.AddDays(-2),
        };
    }
}