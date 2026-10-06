using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Ingredients.GetIngredient;
using CgmLink.Api.Endpoints.Ingredients;
using CgmLink.Api.Services;
using CgmLink.Nutrition;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;
using Endpoint = CgmLink.Api.Endpoints.Ingredients.GetIngredient.Endpoint;

namespace CgmLink.Api.Tests.Endpoints.Ingredients;

[TestFixture]
public class GetIngredientTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;
    private IngredientsService _service;

    [SetUp]
    public void SetUp()
    {
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _service = new IngredientsService(_ingredientsRepositoryMock.Object, new Mock<INutritionCatalog>().Object, new Mock<IRepository<NutritionIngredient>>().Object);

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);
    }

    [Test]
    public async Task HandleAsync_Should_Return_Ok_With_Ingredient_When_Ingredient_Found()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            UserId = _userId,
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        var result = await Endpoint.HandleAsync(ingredientId.ToString(), _currentUserMock.Object,
            _service, CancellationToken.None);

        _ingredientsRepositoryMock.Verify(r => r.GetAll(It.Is<FindOptions>(o => o.IsAsNoTracking)), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Ok<IngredientResponse>>());
        var okResult = result.Result as Ok<IngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.IngredientId, Is.EqualTo(ingredientId));
            Assert.That(okResult.Value.Name, Is.EqualTo("Milk"));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Return_Servings_When_Ingredient_Has_Servings()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            UserId = _userId,
            Servings =
            {
                new IngredientServing
                {
                    Id = Guid.NewGuid(),
                    IngredientId = ingredientId,
                    Description = "1 cup",
                    Calories = 100,
                    Carbs = 10,
                    Protein = 5,
                    Fat = 2,
                    Created = DateTimeOffset.UtcNow,
                }
            },
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        var result = await Endpoint.HandleAsync(ingredientId.ToString(), _currentUserMock.Object,
            _service, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Ok<IngredientResponse>>());
        var okResult = result.Result as Ok<IngredientResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(okResult!.Value.Servings, Has.Count.EqualTo(1));
            Assert.That(okResult.Value.Servings!.First().Description, Is.EqualTo("1 cup"));
            Assert.That(okResult.Value.Servings!.First().Calories, Is.EqualTo(100));
        });
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Not_Found()
    {
        var ingredientId = Guid.NewGuid();

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId.ToString(), _currentUserMock.Object,
                _service, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Is_Not_Owned_By_User()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            UserId = Guid.NewGuid(),
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId.ToString(), _currentUserMock.Object,
                _service, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Ingredient_Is_Soft_Deleted()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            Deleted = DateTimeOffset.UtcNow,
            UserId = _userId,
        };

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient> { ingredient }));

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId.ToString(), _currentUserMock.Object,
                _service, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("INGREDIENT_NOT_FOUND"));
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedAccessException_When_User_Is_Not_Logged_In()
    {
        var ingredientId = Guid.NewGuid();

        _currentUserMock
            .Setup(c => c.GetUserId())
            .Throws<UnauthorizedAccessException>();

        Assert.That(async () => await Endpoint.HandleAsync(ingredientId.ToString(), _currentUserMock.Object,
                _service, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedAccessException>());
    }

    [TestCase("0", IngredientType.Personal)]
    [TestCase("External", IngredientType.External)]
    [TestCase("Personal", IngredientType.Personal)]
    [TestCase("1", IngredientType.External)]
    [TestCase(null, IngredientType.Personal)]
    public async Task HandleAsync_Should_Bind_Ingredient_Type_From_Query(string value, IngredientType expected)
    {
        var userId = Guid.NewGuid();
        var currentUser = new Mock<ICurrentUser>();
        currentUser.Setup(user => user.GetUserId()).Returns(userId);
        var service = new Mock<IIngredientsService>(MockBehavior.Strict);
        service.Setup(item => item.GetIngredientAsync("123", userId, It.IsAny<CancellationToken>(), expected))
            .ReturnsAsync(IngredientResponse.FromIngredient(new Ingredient { Name = "Milk", Created = DateTimeOffset.UtcNow }));
        using var services = new ServiceCollection().AddLogging()
            .AddSingleton(currentUser.Object).AddSingleton(service.Object).BuildServiceProvider();
        var handler = RequestDelegateFactory.Create(CgmLink.Api.Endpoints.Ingredients.GetIngredient.Endpoint.HandleAsync,
            new RequestDelegateFactoryOptions { ServiceProvider = services, RouteParameterNames = ["identifier"] }).RequestDelegate;
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.RouteValues["identifier"] = "123";
        context.Request.QueryString = new QueryString(value is null ? "" : "?type=" + value);
        context.Response.Body = new MemoryStream();

        await handler(context);

        Assert.That(context.Response.StatusCode, Is.EqualTo(200));
        service.VerifyAll();
    }
}
