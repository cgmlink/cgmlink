using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using CgmLink.Api.Endpoints.Ingredients.NewIngredient;
using CgmLink.Data.Entities;
using CgmLink.Data.Enums;
using CgmLink.Data.Repository;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using NUnit.Framework;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class NewIngredientTests
{
    private Mock<IValidator<NewIngredientRequest>> _validatorMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IRepository<Ingredient>> _ingredientRepositoryMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<NewIngredientRequest>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _ingredientRepositoryMock = new Mock<IRepository<Ingredient>>();
    }

    [Test]
    public async Task HandleAsync_Returns_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new NewIngredientRequest { Name = "Test", Carbs = 10, Protein = 5, Fat = 2, Calories = 100, Uom = (Models.UnitOfMeasurement)UnitOfMeasurement.Grams };
        var validationResult = new FluentValidation.Results.ValidationResult([new FluentValidation.Results.ValidationFailure("Name", "Name is required")
        ]);

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public async Task HandleAsync_Returns_Ok_With_Response_When_Request_Is_Valid()
    {
        var request = new NewIngredientRequest { Name = "Test", Carbs = 10, Protein = 5, Fat = 2, Calories = 100, Uom = (Models.UnitOfMeasurement)UnitOfMeasurement.Grams };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var userId = Guid.NewGuid();

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Returns(userId);

        _ingredientRepositoryMock.Setup(r => r.Add(It.IsAny<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<NewIngredientResponse>>());
        var okResult = result.Result as Ok<NewIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Name, Is.EqualTo(request.Name));
            Assert.That(okResult.Value.Carbs, Is.EqualTo(request.Carbs));
            Assert.That(okResult.Value.Protein, Is.EqualTo(request.Protein));
            Assert.That(okResult.Value.Fat, Is.EqualTo(request.Fat));
            Assert.That(okResult.Value.Calories, Is.EqualTo(request.Calories));
            Assert.That(okResult.Value.Uom, Is.EqualTo(request.Uom));
            Assert.That(okResult.Value.Barcode, Is.Null);
        });
    }

    [Test]
    public async Task HandleAsync_Returns_Ok_With_Response_With_Barcode()
    {
        var request = new NewIngredientRequest { Name = "Test", Barcode = "123456789", Carbs = 10, Protein = 5, Fat = 2, Calories = 100, Uom = (Models.UnitOfMeasurement)UnitOfMeasurement.Grams };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var userId = Guid.NewGuid();

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Returns(userId);

        _ingredientRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Ingredient?)null);

        _ingredientRepositoryMock.Setup(r => r.Add(It.IsAny<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.TypeOf<Ok<NewIngredientResponse>>());
            var okResult = result.Result as Ok<NewIngredientResponse>;
            Assert.That(okResult!.Value.Name, Is.EqualTo(request.Name));
            Assert.That(okResult.Value.Barcode, Is.EqualTo("123456789"));
        });
    }

    [Test]
    public async Task HandleAsync_Returns_Conflict_When_Barcode_Already_Exists_For_User()
    {
        var userId = Guid.NewGuid();
        var existingIngredient = new Ingredient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Existing Ingredient",
            Barcode = "123456789",
            Created = DateTimeOffset.UtcNow.AddDays(-1),
            Carbs = 20,
            Protein = 10,
            Fat = 5,
            Calories = 150,
            Uom = UnitOfMeasurement.Grams,
        };

        var request = new NewIngredientRequest { Name = "New Ingredient", Barcode = "123456789", Carbs = 10, Protein = 5, Fat = 2, Calories = 100, Uom = (Models.UnitOfMeasurement)UnitOfMeasurement.Grams };
        var validationResult = new FluentValidation.Results.ValidationResult();

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Returns(userId);

        _ingredientRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingIngredient);

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Conflict<NewIngredientResponse>>());
        var conflictResult = result.Result as Conflict<NewIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(conflictResult!.Value.Id, Is.EqualTo(existingIngredient.Id));
            Assert.That(conflictResult.Value.Name, Is.EqualTo(existingIngredient.Name));
            Assert.That(conflictResult.Value.Barcode, Is.EqualTo(existingIngredient.Barcode));
            Assert.That(conflictResult.Value.Carbs, Is.EqualTo(existingIngredient.Carbs));
            Assert.That(conflictResult.Value.Protein, Is.EqualTo(existingIngredient.Protein));
            Assert.That(conflictResult.Value.Fat, Is.EqualTo(existingIngredient.Fat));
            Assert.That(conflictResult.Value.Calories, Is.EqualTo(existingIngredient.Calories));
            Assert.That(conflictResult.Value.Uom, Is.EqualTo((Models.UnitOfMeasurement)existingIngredient.Uom));
            Assert.That(conflictResult.Value.Created, Is.EqualTo(existingIngredient.Created));
        });

        _ingredientRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task HandleAsync_Does_Not_Check_Duplicate_When_Barcode_Is_Null()
    {
        var request = new NewIngredientRequest { Name = "Test", Barcode = null, Carbs = 10, Protein = 5, Fat = 2, Calories = 100, Uom = (Models.UnitOfMeasurement)UnitOfMeasurement.Grams };
        var validationResult = new FluentValidation.Results.ValidationResult();
        var userId = Guid.NewGuid();

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Returns(userId);

        _ingredientRepositoryMock.Setup(r => r.Add(It.IsAny<Ingredient>()));

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object, _currentUserMock.Object, _ingredientRepositoryMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<NewIngredientResponse>>());
        _ingredientRepositoryMock.Verify(
            r => r.FindOneAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Ingredient, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}