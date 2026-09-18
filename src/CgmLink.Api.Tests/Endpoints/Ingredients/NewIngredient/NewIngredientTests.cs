using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients.NewIngredient;
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

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class NewIngredientTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<NewIngredientRequest>> _validatorMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<IRepository<User>> _usersRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<NewIngredientRequest>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _usersRepositoryMock = new Mock<IRepository<User>>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _usersRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = _userId,
                Email = "test@nomail.com",
                PasswordHash = "password",
            });

        _ingredientsRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ingredientsRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new NewIngredientRequest { Name = "Test" };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Name", "Name is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _ingredientsRepositoryMock.Object, _usersRepositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new NewIngredientRequest { Name = "Test Ingredient" };

        _usersRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _ingredientsRepositoryMock.Object, _usersRepositoryMock.Object, _currentUserMock.Object, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedException>().With.Message.EqualTo("USER_NOT_LOGGED_IN"));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Created_With_Existing_Ingredient_When_Barcode_Matched_And_Already_Linked()
    {
        var existing = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
            Users = { new UserIngredient { UserId = _userId, IngredientId = Guid.NewGuid(), Created = DateTimeOffset.UtcNow } },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { existing }));

        var request = new NewIngredientRequest { Name = "Milk", Barcode = "123" };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _ingredientsRepositoryMock.Object, _usersRepositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Ingredient>(), It.IsAny<CancellationToken>()), Times.Never);

        Assert.That(result.Result, Is.TypeOf<Created<NewIngredientResponse>>());
        var created = result.Result as Created<NewIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Id, Is.EqualTo(existing.Id));
            Assert.That(created.Value.Name, Is.EqualTo(existing.Name));
            Assert.That(created.Value.Barcode, Is.EqualTo(existing.Barcode));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Link_User_To_Existing_Ingredient_When_Barcode_Matched_And_Not_Linked()
    {
        var existing = new Ingredient
        {
            Id = Guid.NewGuid(),
            Name = "Milk",
            Barcode = "123",
            Created = DateTimeOffset.UtcNow,
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { existing }));

        var request = new NewIngredientRequest { Name = "Milk", Barcode = "123" };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _ingredientsRepositoryMock.Object, _usersRepositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.UpdateAsync(It.Is<Ingredient>(i => i.Users.Any(u => u.UserId == _userId)), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Created<NewIngredientResponse>>());
        var created = result.Result as Created<NewIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Id, Is.EqualTo(existing.Id));
            Assert.That(created.Value.Name, Is.EqualTo(existing.Name));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Created_When_Request_Is_Valid()
    {
        var request = new NewIngredientRequest { Name = "Test Ingredient", Barcode = "456" };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _ingredientsRepositoryMock.Object, _usersRepositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.AddAsync(It.Is<Ingredient>(i =>
            i.Name == request.Name &&
            i.Barcode == request.Barcode &&
            i.Users.Any(u => u.UserId == _userId)
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Created<NewIngredientResponse>>());
        var created = result.Result as Created<NewIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Name, Is.EqualTo(request.Name));
            Assert.That(created.Value.Barcode, Is.EqualTo(request.Barcode));
            Assert.That(created.Value.Created, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(created.Value.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Add_Servings_When_Request_Has_Servings()
    {
        var request = new NewIngredientRequest
        {
            Name = "Test Ingredient",
            Servings =
            {
                new NewIngredientRequest.NewIngredientServingRequest
                {
                    Description = "1 cup",
                    Calories = 100,
                    Carbs = 10,
                    Protein = 5,
                    Fat = 2,
                }
            }
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _ingredientsRepositoryMock.Object, _usersRepositoryMock.Object, _currentUserMock.Object, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.AddAsync(It.Is<Ingredient>(i => i.Servings.Count == 1), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Created<NewIngredientResponse>>());
        var created = result.Result as Created<NewIngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Servings, Has.Count.EqualTo(1));
            Assert.That(created.Value.Servings!.First().Description, Is.EqualTo("1 cup"));
            Assert.That(created.Value.Servings!.First().Calories, Is.EqualTo(100));
        });
    }
}