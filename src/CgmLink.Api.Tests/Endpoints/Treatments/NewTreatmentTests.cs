using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Treatments.NewTreatment;
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
public class NewTreatmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<NewTreatmentRequest>> _validatorMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IRepository<User>> _usersRepositoryMock;
    private Mock<IRepository<Reading>> _readingsRepositoryMock;
    private Mock<IRepository<Insulin>> _insulinsRepositoryMock;
    private Mock<IRepository<Injection>> _injectionsRepositoryMock;
    private Mock<IRepository<Treatment>> _treatmentsRepositoryMock;
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private IMealService _mealService;
    private IIngredientsService _ingredientsService;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<NewTreatmentRequest>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _usersRepositoryMock = new Mock<IRepository<User>>();
        _readingsRepositoryMock = new Mock<IRepository<Reading>>();
        _insulinsRepositoryMock = new Mock<IRepository<Insulin>>();
        _injectionsRepositoryMock = new Mock<IRepository<Injection>>();
        _treatmentsRepositoryMock = new Mock<IRepository<Treatment>>();
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _mealService = new MealService(_mealsRepositoryMock.Object);
        _ingredientsService = new IngredientsService(_ingredientsRepositoryMock.Object);

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<NewTreatmentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _usersRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = _userId,
                Email = "test@nomail.com",
                PasswordHash = "password",
            });

        _treatmentsRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _injectionsRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

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
            Direction = ReadingDirection.Steady,
            GlucoseLevel = 5.0,
        };
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
        var request = new NewTreatmentRequest();

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Meals", "At least one is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
            _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
            _mealService, _ingredientsService, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new NewTreatmentRequest
        {
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m } }
        };

        _usersRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedException>().With.Message.EqualTo("USER_NOT_LOGGED_IN"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Insulin_Not_Found()
    {
        var request = new NewTreatmentRequest
        {
            Injection = new NewTreatmentRequest.NewInjectionRequest { InsulinId = Guid.NewGuid(), Units = 5m }
        };

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Insulin?)null);

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INSULIN_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Meal_Not_Found()
    {
        var request = new NewTreatmentRequest
        {
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m } }
        };

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("MEAL_ID_INVALID"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Ingredient_Not_Found()
    {
        var request = new NewTreatmentRequest
        {
            Ingredients =
            {
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1m,
                }
            }
        };

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Reading_Not_Found()
    {
        var request = new NewTreatmentRequest
        {
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m } },
            ReadingId = Guid.NewGuid(),
        };

        var meal = CreateMeal(request.Meals.Single().MealId);
        SetupMeals(new List<Meal> { meal });

        _readingsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Reading, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reading?)null);

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("READING_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Not_Add_Injection_When_Meal_Not_Found()
    {
        var insulinId = Guid.NewGuid();

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInsulin(insulinId));

        var request = new NewTreatmentRequest
        {
            Injection = new NewTreatmentRequest.NewInjectionRequest { InsulinId = insulinId, Units = 5m },
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m } }
        };

        _mealsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Meal>(new List<Meal>()));

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("MEAL_ID_INVALID"));

        _injectionsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()), Times.Never);
        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Not_Add_Injection_When_Reading_Not_Found()
    {
        var mealId = Guid.NewGuid();
        var insulinId = Guid.NewGuid();

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInsulin(insulinId));

        SetupMeals(new List<Meal> { CreateMeal(mealId) });

        _readingsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Reading, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reading?)null);

        var request = new NewTreatmentRequest
        {
            Injection = new NewTreatmentRequest.NewInjectionRequest { InsulinId = insulinId, Units = 5m },
            ReadingId = Guid.NewGuid(),
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId, Quantity = 1m } }
        };

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
                _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
                _mealService, _ingredientsService, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("READING_NOT_FOUND"));

        _injectionsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()), Times.Never);
        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Treatment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_Should_Create_Injection_And_Treatment_When_Request_Is_Valid()
    {
        var mealId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var readingId = Guid.NewGuid();
        var insulinId = Guid.NewGuid();

        var meal = CreateMeal(mealId);
        SetupMeals(new List<Meal> { meal });

        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId, ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInsulin(insulinId));

        _readingsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Reading, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateReading(readingId));

        var request = new NewTreatmentRequest
        {
            Injection = new NewTreatmentRequest.NewInjectionRequest { InsulinId = insulinId, Units = 5m },
            ReadingId = readingId,
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId, Quantity = 2m } },
            Ingredients =
            {
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = servingId,
                    Quantity = 1m,
                }
            }
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
            _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
            _mealService, _ingredientsService, CancellationToken.None);

        _injectionsRepositoryMock.Verify(r => r.AddAsync(It.Is<Injection>(i =>
            i.UserId == _userId &&
            i.InsulinId == insulinId &&
            i.Units == 5m
        ), It.IsAny<CancellationToken>()), Times.Once);

        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.Is<Treatment>(t =>
            t.UserId == _userId &&
            t.ReadingId == readingId &&
            t.InjectionId != null &&
            t.Calories == 300m &&
            t.Carbs == 30m &&
            t.Protein == 15m &&
            t.Fat == 6m &&
            t.Meals.Count == 1 &&
            t.Meals.Single().MealId == mealId &&
            t.Meals.Single().Quantity == 2m &&
            t.Ingredients.Count == 1 &&
            t.Ingredients.Single().IngredientId == ingredientId &&
            t.Ingredients.Single().ServingId == servingId &&
            t.Ingredients.Single().Quantity == 1m
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Created<NewTreatmentResponse>>());
        var created = result.Result as Created<NewTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.Value.Created, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(created.Value.Updated, Is.Null);
            Assert.That(created.Value.Calories, Is.EqualTo(300m));
            Assert.That(created.Value.Carbs, Is.EqualTo(30m));
            Assert.That(created.Value.Protein, Is.EqualTo(15m));
            Assert.That(created.Value.Fat, Is.EqualTo(6m));
            Assert.That(created.Value.InsulinName, Is.EqualTo("Fiasp"));
            Assert.That(created.Value.InsulinUnits, Is.EqualTo(5m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Create_Treatment_Without_Injection_When_Injection_Is_Null()
    {
        var request = new NewTreatmentRequest
        {
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = Guid.NewGuid(), Quantity = 1m } }
        };

        var meal = CreateMeal(request.Meals.Single().MealId);
        SetupMeals(new List<Meal> { meal });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
            _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
            _mealService, _ingredientsService, CancellationToken.None);

        _injectionsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Injection>(), It.IsAny<CancellationToken>()), Times.Never);

        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.Is<Treatment>(t =>
            t.InjectionId == null &&
            t.Calories == 100m &&
            t.Carbs == 10m &&
            t.Protein == 5m &&
            t.Fat == 2m &&
            t.Meals.Count == 1
        ), It.IsAny<CancellationToken>()), Times.Once);

        var created = result.Result as Created<NewTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.InsulinName, Is.Null);
            Assert.That(created.Value.InsulinUnits, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Zero_Nutrition_When_Only_An_Injection_Is_Provided()
    {
        var insulinId = Guid.NewGuid();

        _insulinsRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Insulin, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInsulin(insulinId));

        var request = new NewTreatmentRequest
        {
            Injection = new NewTreatmentRequest.NewInjectionRequest { InsulinId = insulinId, Units = 3m }
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
            _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
            _mealService, _ingredientsService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.Is<Treatment>(t =>
            t.Calories == 0m &&
            t.Carbs == 0m &&
            t.Protein == 0m &&
            t.Fat == 0m &&
            t.Meals.Count == 0 &&
            t.Ingredients.Count == 0
        ), It.IsAny<CancellationToken>()), Times.Once);

        var created = result.Result as Created<NewTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.InsulinName, Is.EqualTo("Fiasp"));
            Assert.That(created.Value.InsulinUnits, Is.EqualTo(3m));
            Assert.That(created.Value.Calories, Is.EqualTo(0m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Use_Requested_Created_Date()
    {
        var mealId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow.AddDays(-1);
        var request = new NewTreatmentRequest
        {
            Created = created,
            Meals = { new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId, Quantity = 1m } }
        };

        SetupMeals(new List<Meal> { CreateMeal(mealId) });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
            _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
            _mealService, _ingredientsService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.Is<Treatment>(t =>
            t.Created == created &&
            t.Meals.Single().Created == created
        ), It.IsAny<CancellationToken>()), Times.Once);

        var newTreatment = result.Result as Created<NewTreatmentResponse>;
        Assert.That(newTreatment!.Value.Created, Is.EqualTo(created));
    }

    [Test]
    public async Task HandleAsync_Should_Sum_Nutrition_From_Multiple_Meals_And_Ingredients()
    {
        var mealId1 = Guid.NewGuid();
        var mealId2 = Guid.NewGuid();
        var ingredientId1 = Guid.NewGuid();
        var ingredientId2 = Guid.NewGuid();
        var servingId1 = Guid.NewGuid();
        var servingId2 = Guid.NewGuid();

        var meal1 = CreateMeal(mealId1);
        var meal2 = CreateMeal(mealId2);
        meal2.Calories = 50m;
        meal2.Carbs = 5m;
        meal2.Protein = 3m;
        meal2.Fat = 1m;
        SetupMeals(new List<Meal> { meal1, meal2 });

        var ingredient1 = CreateIngredient(ingredientId1);
        ingredient1.Servings.Add(CreateServing(servingId1, ingredientId1));
        var ingredient2 = CreateIngredient(ingredientId2);
        var serving2 = CreateServing(servingId2, ingredientId2);
        serving2.Calories = 50m;
        serving2.Carbs = 5m;
        serving2.Protein = 3m;
        serving2.Fat = 1m;
        ingredient2.Servings.Add(serving2);
        SetupIngredients(new List<Ingredient> { ingredient1, ingredient2 });

        var request = new NewTreatmentRequest
        {
            Meals =
            {
                new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId1, Quantity = 1m },
                new NewTreatmentRequest.NewTreatmentMealRequest { MealId = mealId2, Quantity = 2m },
            },
            Ingredients =
            {
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = ingredientId1,
                    ServingId = servingId1,
                    Quantity = 1m,
                },
                new NewTreatmentRequest.NewTreatmentIngredientRequest
                {
                    IngredientId = ingredientId2,
                    ServingId = servingId2,
                    Quantity = 2m,
                }
            }
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _currentUserMock.Object, _usersRepositoryMock.Object, _readingsRepositoryMock.Object,
            _insulinsRepositoryMock.Object, _injectionsRepositoryMock.Object, _treatmentsRepositoryMock.Object,
            _mealService, _ingredientsService, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.AddAsync(It.Is<Treatment>(t =>
            t.Calories == 400m &&
            t.Carbs == 40m &&
            t.Protein == 22m &&
            t.Fat == 8m &&
            t.Meals.Count == 2 &&
            t.Ingredients.Count == 2
        ), It.IsAny<CancellationToken>()), Times.Once);

        var created = result.Result as Created<NewTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Calories, Is.EqualTo(400m));
            Assert.That(created.Value.Carbs, Is.EqualTo(40m));
            Assert.That(created.Value.Protein, Is.EqualTo(22m));
            Assert.That(created.Value.Fat, Is.EqualTo(8m));
        });
    }
}