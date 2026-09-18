using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Treatments.GetTreatment;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Treatments;

[TestFixture]
public class GetTreatmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Treatment>> _treatmentsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _treatmentsRepositoryMock = new Mock<IRepository<Treatment>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _treatmentsRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
    }

    private Reading CreateReading(Guid id)
    {
        return new Reading
        {
            Id = id,
            UserId = _userId,
            Created = DateTimeOffset.UtcNow.AddHours(-1),
            GlucoseLevel = 5.0,
            Direction = ReadingDirection.Steady,
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

    private IngredientServing CreateServing(Guid id, Guid ingredientId)
    {
        return new IngredientServing
        {
            Id = id,
            IngredientId = ingredientId,
            Description = "1 cup",
            ServingAmount = 250,
            ServingUnit = "ml",
            Calories = 100m,
            Carbs = 10m,
            Protein = 5m,
            Fat = 2m,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private TreatmentMeal CreateTreatmentMeal(Guid treatmentId, Guid mealId, decimal quantity)
    {
        return new TreatmentMeal
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            MealId = mealId,
            Meal = new Meal
            {
                Id = mealId,
                UserId = _userId,
                Name = "Breakfast",
                Calories = 100m,
                Carbs = 10m,
                Protein = 5m,
                Fat = 2m,
                Created = DateTimeOffset.UtcNow.AddDays(-1),
            },
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private TreatmentIngredient CreateTreatmentIngredient(Guid treatmentId, Guid ingredientId, Guid servingId, decimal quantity)
    {
        return new TreatmentIngredient
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            IngredientId = ingredientId,
            Ingredient = new Ingredient
            {
                Id = ingredientId,
                Name = "Milk",
                Barcode = "123",
                Created = DateTimeOffset.UtcNow,
            },
            ServingId = servingId,
            Serving = CreateServing(servingId, ingredientId),
            Quantity = quantity,
            Created = DateTimeOffset.UtcNow,
        };
    }

    private Treatment CreateTreatment(Guid id)
    {
        var reading = CreateReading(Guid.NewGuid());
        var insulin = CreateInsulin(Guid.NewGuid());
        var injection = new Injection
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            InsulinId = insulin.Id,
            Insulin = insulin,
            Units = 5m,
            Created = DateTimeOffset.UtcNow,
        };

        return new Treatment
        {
            Id = id,
            UserId = _userId,
            ReadingId = reading.Id,
            Reading = reading,
            InjectionId = injection.Id,
            Injection = injection,
            Calories = 300m,
            Carbs = 30m,
            Protein = 15m,
            Fat = 6m,
            Created = DateTimeOffset.UtcNow.AddDays(-2),
            Updated = DateTimeOffset.UtcNow.AddDays(-1),
        };
    }

    private void SetupTreatment(Treatment treatment)
    {
        _treatmentsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(new List<Treatment> { treatment }));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Treatment_Details_When_Treatment_Found()
    {
        var treatmentId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow.AddDays(-2);
        var updated = DateTimeOffset.UtcNow.AddDays(-1);

        var treatment = CreateTreatment(treatmentId);
        treatment.Created = created;
        treatment.Updated = updated;
        treatment.Injection!.Units = 5m;
        treatment.Meals.Add(CreateTreatmentMeal(treatmentId, mealId, 2m));
        treatment.Ingredients.Add(CreateTreatmentIngredient(treatmentId, ingredientId, servingId, 1m));
        SetupTreatment(treatment);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, CancellationToken.None);

        _treatmentsRepositoryMock.Verify(r => r.GetAll(It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);
        _treatmentsRepositoryMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<GetTreatmentResponse>>());
        var okResult = result.Result as Ok<GetTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Id, Is.EqualTo(treatmentId));
            Assert.That(okResult.Value.Created, Is.EqualTo(created));
            Assert.That(okResult.Value.Updated, Is.EqualTo(updated));
            Assert.That(okResult.Value.Calories, Is.EqualTo(300m));
            Assert.That(okResult.Value.Carbs, Is.EqualTo(30m));
            Assert.That(okResult.Value.Protein, Is.EqualTo(15m));
            Assert.That(okResult.Value.Fat, Is.EqualTo(6m));

            Assert.That(okResult.Value.Reading, Is.Not.Null);
            Assert.That(okResult.Value.Reading!.GlucoseLevel, Is.EqualTo(5.0));
            Assert.That(okResult.Value.Reading.Direction, Is.EqualTo(CgmLink.Api.Models.ReadingDirection.Steady));

            Assert.That(okResult.Value.InsulinName, Is.EqualTo("Fiasp"));
            Assert.That(okResult.Value.InsulinUnits, Is.EqualTo(5m));

            Assert.That(okResult.Value.Meals, Has.Count.EqualTo(1));
            var mealResponse = okResult.Value.Meals;
            Assert.That(mealResponse.Single().MealId, Is.EqualTo(mealId));
            Assert.That(mealResponse.Single().Name, Is.EqualTo("Breakfast"));
            Assert.That(mealResponse.Single().Quantity, Is.EqualTo(2m));
            Assert.That(mealResponse.Single().Calories, Is.EqualTo(200m));
            Assert.That(mealResponse.Single().Carbs, Is.EqualTo(20m));
            Assert.That(mealResponse.Single().Protein, Is.EqualTo(10m));
            Assert.That(mealResponse.Single().Fat, Is.EqualTo(4m));

            Assert.That(okResult.Value.Ingredients, Has.Count.EqualTo(1));
            var ingredientResponse = okResult.Value.Ingredients.Single();
            Assert.That(ingredientResponse.IngredientId, Is.EqualTo(ingredientId));
            Assert.That(ingredientResponse.IngredientName, Is.EqualTo("Milk"));
            Assert.That(ingredientResponse.Barcode, Is.EqualTo("123"));
            Assert.That(ingredientResponse.Serving, Is.Not.Null);
            Assert.That(ingredientResponse.Serving!.Description, Is.EqualTo("1 cup"));
            Assert.That(ingredientResponse.Serving.ServingAmount, Is.EqualTo(250));
            Assert.That(ingredientResponse.Serving.ServingUnit, Is.EqualTo("ml"));
            Assert.That(ingredientResponse.Serving.Calories, Is.EqualTo(100m));
            Assert.That(ingredientResponse.Quantity, Is.EqualTo(1m));
            Assert.That(ingredientResponse.Calories, Is.EqualTo(100m));
            Assert.That(ingredientResponse.Carbs, Is.EqualTo(10m));
            Assert.That(ingredientResponse.Protein, Is.EqualTo(5m));
            Assert.That(ingredientResponse.Fat, Is.EqualTo(2m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Meal_Snapshot_Nutrition_Without_Ingredients()
    {
        var treatmentId = Guid.NewGuid();
        var mealId = Guid.NewGuid();

        var treatment = CreateTreatment(treatmentId);
        var treatmentMeal = CreateTreatmentMeal(treatmentId, mealId, 2m);
        treatmentMeal.Meal!.Ingredients.Add(new MealIngredient
        {
            Id = Guid.NewGuid(),
            MealId = mealId,
            IngredientId = Guid.NewGuid(),
            Quantity = 1m,
            Created = DateTimeOffset.UtcNow,
        });
        treatment.Meals.Add(treatmentMeal);
        SetupTreatment(treatment);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<GetTreatmentResponse>;
        var mealResponse = okResult!.Value.Meals.Single();
        Assert.Multiple(() =>
        {
            Assert.That(mealResponse.MealId, Is.EqualTo(mealId));
            Assert.That(mealResponse.Name, Is.EqualTo("Breakfast"));
            Assert.That(mealResponse.Quantity, Is.EqualTo(2m));
            Assert.That(mealResponse.Calories, Is.EqualTo(200m));
            Assert.That(mealResponse.Carbs, Is.EqualTo(20m));
            Assert.That(mealResponse.Protein, Is.EqualTo(10m));
            Assert.That(mealResponse.Fat, Is.EqualTo(4m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Empty_Meals_And_Ingredients_When_Treatment_Has_None()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.Calories = 0m;
        treatment.Carbs = 0m;
        treatment.Protein = 0m;
        treatment.Fat = 0m;
        SetupTreatment(treatment);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<GetTreatmentResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Meals, Is.Empty);
            Assert.That(okResult.Value.Ingredients, Is.Empty);
            Assert.That(okResult.Value.Calories, Is.EqualTo(0m));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Zero_Nutrition_When_Serving_Not_Loaded()
    {
        var treatmentId = Guid.NewGuid();
        var ingredientId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);

        var treatmentIngredient = new TreatmentIngredient
        {
            Id = Guid.NewGuid(),
            TreatmentId = treatmentId,
            IngredientId = ingredientId,
            Ingredient = new Ingredient
            {
                Id = ingredientId,
                Name = "Milk",
                Barcode = "123",
                Created = DateTimeOffset.UtcNow,
            },
            ServingId = Guid.NewGuid(),
            Serving = null,
            Quantity = 2m,
            Created = DateTimeOffset.UtcNow,
        };
        treatment.Ingredients.Add(treatmentIngredient);
        SetupTreatment(treatment);

        var result = await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
            _treatmentsRepositoryMock.Object, CancellationToken.None);

        var okResult = result.Result as Ok<GetTreatmentResponse>;
        var ingredientResponse = okResult!.Value.Ingredients.Single();
        Assert.Multiple(() =>
        {
            Assert.That(ingredientResponse.Serving, Is.Null);
            Assert.That(ingredientResponse.Quantity, Is.EqualTo(2m));
            Assert.That(ingredientResponse.Calories, Is.EqualTo(0m));
            Assert.That(ingredientResponse.Carbs, Is.EqualTo(0m));
            Assert.That(ingredientResponse.Protein, Is.EqualTo(0m));
            Assert.That(ingredientResponse.Fat, Is.EqualTo(0m));
        });
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Not_Found()
    {
        var treatmentId = Guid.NewGuid();

        _treatmentsRepositoryMock
            .Setup(r => r.AnyAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _treatmentsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Treatment>(new List<Treatment>()));

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));

        _treatmentsRepositoryMock.Verify(r => r.GetAll(It.IsAny<FindOptions>()), Times.Never);
        _treatmentsRepositoryMock.Verify(r => r.AnyAsync(It.IsAny<Expression<Func<Treatment, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Not_Linked_To_User()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.UserId = Guid.NewGuid();
        SetupTreatment(treatment);

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Treatment_Is_Soft_Deleted()
    {
        var treatmentId = Guid.NewGuid();
        var treatment = CreateTreatment(treatmentId);
        treatment.Deleted = DateTimeOffset.UtcNow;
        SetupTreatment(treatment);

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("TREATMENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        var treatmentId = Guid.NewGuid();

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(treatmentId, _currentUserMock.Object,
                _treatmentsRepositoryMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }
}