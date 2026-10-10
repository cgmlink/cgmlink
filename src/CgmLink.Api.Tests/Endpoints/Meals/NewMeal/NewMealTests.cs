using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Api.Endpoints.Meals.NewMeal;
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
public class NewMealTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IValidator<NewMealRequest>> _validatorMock;
    private Mock<IRepository<Meal>> _mealsRepositoryMock;
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<IRepository<User>> _usersRepositoryMock;
    private Mock<ICurrentUser> _currentUserMock;
    private Mock<IRepository<NutritionIngredient>> _nutritionIngredientsRepositoryMock;
    private Mock<CgmLink.Nutrition.INutritionCatalog> _nutritionCatalogMock;
    private List<NutritionIngredient> _nutritionIdentities;
    private IIngredientsService _ingredientsService;
    private IMealService _mealService;

    [SetUp]
    public void SetUp()
    {
        _validatorMock = new Mock<IValidator<NewMealRequest>>();
        _mealsRepositoryMock = new Mock<IRepository<Meal>>();
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _usersRepositoryMock = new Mock<IRepository<User>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _nutritionIngredientsRepositoryMock = new Mock<IRepository<NutritionIngredient>>();
        _nutritionCatalogMock = new Mock<CgmLink.Nutrition.INutritionCatalog>();
        _nutritionCatalogMock.SetupGet(c => c.Source).Returns("external");
        _nutritionIdentities = [];
        _nutritionIngredientsRepositoryMock.Setup(r => r.GetAll(null))
            .Returns(() => new TestAsyncEnumerable<NutritionIngredient>(_nutritionIdentities));
        _nutritionIngredientsRepositoryMock
            .Setup(r => r.AddManyAsync(It.IsAny<IEnumerable<NutritionIngredient>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NutritionIngredient>, CancellationToken>((identities, _) => _nutritionIdentities.AddRange(identities))
            .Returns(Task.CompletedTask);
        _ingredientsService = new IngredientsService(
            _ingredientsRepositoryMock.Object,
            _nutritionCatalogMock.Object,
            _nutritionIngredientsRepositoryMock.Object);
        _mealService = new MealService(_mealsRepositoryMock.Object, _nutritionCatalogMock.Object, _ingredientsService, _nutritionIngredientsRepositoryMock.Object);

        _currentUserMock.Setup(c => c.GetUserId()).Returns(_userId);

        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<NewMealRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        _usersRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Patient
            {
                Id = _userId,
                Email = "test@nomail.com",
                PasswordHash = "password",
            });

        _mealsRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(new List<Ingredient>()));
    }

    private Ingredient CreateIngredient(Guid id)
    {
        return new Ingredient
        {
            Id = id,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            UserId = _userId,
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

    private void SetupIngredients(IEnumerable<Ingredient> ingredients)
    {
        _ingredientsRepositoryMock
            .Setup(r => r.GetAll())
            .Returns(new TestAsyncEnumerable<Ingredient>(ingredients.ToList()));
    }

    [Test]
    public async Task HandleAsync_Should_Return_ValidationProblem_When_Request_Is_Invalid()
    {
        var request = new NewMealRequest { Name = "" };

        _validatorMock
            .Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult
            {
                Errors = { new ValidationFailure("Name", "Name is required.") }
            });

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _mealsRepositoryMock.Object,
            _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<ValidationProblem>());
    }

    [Test]
    public void HandleAsync_Should_Throw_UnauthorizedException_When_User_Is_Not_Logged_In()
    {
        var request = new NewMealRequest { Name = "Breakfast" };

        _usersRepositoryMock
            .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<User, bool>>>(), It.IsAny<FindOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _mealsRepositoryMock.Object,
                _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None),
            Throws.InstanceOf<UnauthorizedException>().With.Message.EqualTo("USER_NOT_LOGGED_IN"));
    }

    [Test]
    public async Task HandleAsync_Should_Return_Created_When_Request_Is_Valid()
    {
        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId, ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            {
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = servingId,
                    Quantity = 2,
                }
            }
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _mealsRepositoryMock.Object,
            _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.AddAsync(It.Is<Meal>(m =>
            m.Name == request.Name &&
            m.UserId == _userId &&
            m.Calories == 200 &&
            m.Carbs == 20 &&
            m.Protein == 10 &&
            m.Fat == 4 &&
            m.Ingredients.Count == 1
        ), It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result.Result, Is.TypeOf<Created<NewMealResponse>>());
        var created = result.Result as Created<NewMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Name, Is.EqualTo(request.Name));
            Assert.That(created.Value.Calories, Is.EqualTo(200));
            Assert.That(created.Value.Carbs, Is.EqualTo(20));
            Assert.That(created.Value.Protein, Is.EqualTo(10));
            Assert.That(created.Value.Fat, Is.EqualTo(4));
            Assert.That(created.Value.IngredientCount, Is.EqualTo(1));
            Assert.That(created.Value.Created, Is.EqualTo(DateTimeOffset.UtcNow).Within(TimeSpan.FromSeconds(1)));
            Assert.That(created.Value.Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public async Task HandleAsync_Should_Persist_Nutrition_From_Meal_Service_When_Request_Has_Multiple_Ingredients()
    {
        var ingredientId1 = Guid.NewGuid();
        var ingredientId2 = Guid.NewGuid();
        var servingId1 = Guid.NewGuid();
        var servingId2 = Guid.NewGuid();

        var ingredient1 = CreateIngredient(ingredientId1);
        ingredient1.Servings.Add(CreateServing(servingId1, ingredientId1));
        var ingredient2 = CreateIngredient(ingredientId2);
        ingredient2.Servings.Add(CreateServing(servingId2, ingredientId2));
        SetupIngredients(new List<Ingredient> { ingredient1, ingredient2 });

        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            {
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = ingredientId1,
                    ServingId = servingId1,
                    Quantity = 2,
                },
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = ingredientId2,
                    ServingId = servingId2,
                    Quantity = 1,
                }
            }
        };

        var result = await Endpoint.HandleAsync(request, _validatorMock.Object,
            _mealsRepositoryMock.Object,
            _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None);

        _mealsRepositoryMock.Verify(r => r.AddAsync(It.Is<Meal>(m =>
            m.Calories == 300 &&
            m.Carbs == 30 &&
            m.Protein == 15 &&
            m.Fat == 6 &&
            m.Ingredients.Count == 2
        ), It.IsAny<CancellationToken>()), Times.Once);

        var created = result.Result as Created<NewMealResponse>;
        Assert.Multiple(() =>
        {
            Assert.That(created!.Value.Calories, Is.EqualTo(300));
            Assert.That(created.Value.Carbs, Is.EqualTo(30));
            Assert.That(created.Value.Protein, Is.EqualTo(15));
            Assert.That(created.Value.Fat, Is.EqualTo(6));
            Assert.That(created.Value.IngredientCount, Is.EqualTo(2));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task HandleAsync_Should_Create_Meal_With_Nutrition_Ingredients(bool includePersonalIngredient)
    {
        _nutritionCatalogMock.Setup(c => c.GetAsync("product-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CgmLink.Nutrition.Source.NutritionProduct
            {
                ProductId = "product-1",
                Name = "External milk",
                Servings = [new CgmLink.Nutrition.Source.NutritionServing
                {
                    ExternalId = "serving-1", Calories = 100, Carbs = 10, Protein = 5, Fat = 2,
                }],
            });
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            NutritionIngredients = [new NewMealRequest.NewMealNutritionIngredientRequest
            {
                ProductId = "product-1", ServingId = "serving-1", Quantity = 2,
            }],
        };
        if (includePersonalIngredient)
        {
            var ingredient = CreateIngredient(Guid.NewGuid());
            var serving = CreateServing(Guid.NewGuid(), ingredient.Id);
            ingredient.Servings.Add(serving);
            SetupIngredients([ingredient]);
            request.Ingredients.Add(new NewMealRequest.NewMealIngredientRequest
            {
                IngredientId = ingredient.Id,
                ServingId = serving.Id,
                Quantity = 1,
            });
        }
        Meal saved = null;
        _mealsRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()))
            .Callback<Meal, CancellationToken>((meal, _) => saved = meal)
            .Returns(Task.CompletedTask);

        var result = await Endpoint.HandleAsync(request, new NewMealRequest.NewMealRequestValidator(),
            _mealsRepositoryMock.Object, _usersRepositoryMock.Object,
            _currentUserMock.Object, _mealService, CancellationToken.None);

        Assert.That(result.Result, Is.TypeOf<Created<NewMealResponse>>());
        var response = ((Created<NewMealResponse>)result.Result).Value;
        var identity = _nutritionIdentities.Single();
        Assert.Multiple(() =>
        {
            Assert.That(saved.NutritionIngredients.Single().NutritionIngredientId, Is.EqualTo(identity.Id));
            Assert.That(saved.NutritionIngredients.Single().ServingId, Is.EqualTo(identity.Servings.Single().Id));
            Assert.That(saved.NutritionIngredients.Single().Quantity, Is.EqualTo(2));
            Assert.That(response.IngredientCount, Is.EqualTo(includePersonalIngredient ? 2 : 1));
            Assert.That(response.Calories, Is.EqualTo(includePersonalIngredient ? 300 : 200));
            Assert.That(response.Carbs, Is.EqualTo(includePersonalIngredient ? 30 : 20));
            Assert.That(response.Protein, Is.EqualTo(includePersonalIngredient ? 15 : 10));
            Assert.That(response.Fat, Is.EqualTo(includePersonalIngredient ? 6 : 4));
        });
    }

    [TestCase("missing-product", "serving-1")]
    [TestCase("product-1", "missing-serving")]
    public void HandleAsync_Should_Reject_Invalid_Nutrition_References(string productId, string servingId)
    {
        _nutritionCatalogMock.Setup(c => c.GetAsync("product-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CgmLink.Nutrition.Source.NutritionProduct
            {
                ProductId = "product-1",
                Name = "Milk",
                Servings = [new CgmLink.Nutrition.Source.NutritionServing { ExternalId = "serving-1" }],
            });
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            NutritionIngredients = [new NewMealRequest.NewMealNutritionIngredientRequest
            {
                ProductId = productId, ServingId = servingId, Quantity = 1,
            }],
        };
        Assert.That(async () => await Endpoint.HandleAsync(request, new NewMealRequest.NewMealRequestValidator(),
            _mealsRepositoryMock.Object, _usersRepositoryMock.Object,
            _currentUserMock.Object, _mealService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>());
        _mealsRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Meal>(), It.IsAny<CancellationToken>()), Times.Never);
        _nutritionIngredientsRepositoryMock.Verify(r => r.AddManyAsync(It.IsAny<IEnumerable<NutritionIngredient>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Ingredient_Not_Found()
    {
        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            {
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = Guid.NewGuid(),
                    ServingId = Guid.NewGuid(),
                    Quantity = 1,
                }
            }
        };

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _mealsRepositoryMock.Object,
                _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Ingredient_Not_Linked_To_User()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = new Ingredient
        {
            Id = ingredientId,
            Name = "Milk",
            Created = DateTimeOffset.UtcNow,
            UserId = Guid.NewGuid(),
        };
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            {
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1,
                }
            }
        };

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _mealsRepositoryMock.Object,
                _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void HandleAsync_Should_Throw_BadRequestException_When_Serving_Not_Found_On_Ingredient()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(Guid.NewGuid(), ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        var request = new NewMealRequest
        {
            Name = "Breakfast",
            Ingredients =
            {
                new NewMealRequest.NewMealIngredientRequest
                {
                    IngredientId = ingredientId,
                    ServingId = Guid.NewGuid(),
                    Quantity = 1,
                }
            }
        };

        Assert.That(async () => await Endpoint.HandleAsync(request, _validatorMock.Object,
                _mealsRepositoryMock.Object,
                _usersRepositoryMock.Object, _currentUserMock.Object, _mealService, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }
}
