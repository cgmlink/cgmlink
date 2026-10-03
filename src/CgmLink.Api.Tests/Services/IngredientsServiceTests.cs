using CgmLink.Api.Services;
using CgmLink.AspNetCore.Exceptions;
using CgmLink.Data.Entities;
using CgmLink.Data.Repository;
using CgmLink.Data.Tests;
using CgmLink.Nutrition;
using CgmLink.Nutrition.Source;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Api.Tests.Services;

[TestFixture]
public class IngredientsServiceTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private Mock<IRepository<Ingredient>> _ingredientsRepositoryMock;
    private Mock<IRepository<NutritionIngredient>> _nutritionIngredientsRepositoryMock;
    private Mock<INutritionCatalog> _nutritionCatalogMock;
    private List<NutritionIngredient> _nutritionIdentities;
    private IngredientsService _service;

    [SetUp]
    public void SetUp()
    {
        _ingredientsRepositoryMock = new Mock<IRepository<Ingredient>>();
        _nutritionIngredientsRepositoryMock = new Mock<IRepository<NutritionIngredient>>();
        _nutritionCatalogMock = new Mock<INutritionCatalog>();
        _nutritionIdentities = [];
        _nutritionCatalogMock.SetupGet(catalog => catalog.Source).Returns("fatsecret");
        _nutritionIngredientsRepositoryMock.Setup(repository => repository.GetAll(null))
            .Returns(() => new TestAsyncEnumerable<NutritionIngredient>(_nutritionIdentities));
        _nutritionIngredientsRepositoryMock
            .Setup(repository => repository.AddManyAsync(It.IsAny<IEnumerable<NutritionIngredient>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<NutritionIngredient>, CancellationToken>((identities, _) => _nutritionIdentities.AddRange(identities))
            .Returns(Task.CompletedTask);
        _nutritionIngredientsRepositoryMock
            .Setup(repository => repository.UpdateAsync(It.IsAny<NutritionIngredient>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _service = new IngredientsService(
            _ingredientsRepositoryMock.Object,
            _nutritionCatalogMock.Object,
            _nutritionIngredientsRepositoryMock.Object);
    }

    [Test]
    public void Constructor_Should_Reject_Null_Dependencies()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                () => new IngredientsService(null, _nutritionCatalogMock.Object, _nutritionIngredientsRepositoryMock.Object),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("ingredientsRepository"));
            Assert.That(
                () => new IngredientsService(_ingredientsRepositoryMock.Object, null, _nutritionIngredientsRepositoryMock.Object),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("nutritionCatalog"));
            Assert.That(
                () => new IngredientsService(_ingredientsRepositoryMock.Object, _nutritionCatalogMock.Object, null),
                Throws.ArgumentNullException.With.Property("ParamName").EqualTo("nutritionIngredientsRepository"));
        });
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
    public async Task ResolveIngredientsAsync_Should_Return_Local_Ingredient_Nutrition()
    {
        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId, ingredientId));
        SetupIngredients([ingredient]);

        var result = await _service.ResolveIngredientsAsync(
            [new IngredientReference(ingredientId, null, servingId.ToString(), 2m)],
            _userId,
            CancellationToken.None);

        Assert.That(result.Single(), Is.EqualTo(new ResolvedIngredient(
            ingredientId,
            servingId,
            2m,
            100m,
            10m,
            5m,
            2m)));
    }

    [Test]
    public void ResolveIngredientsAsync_Should_Reject_Invalid_Local_Serving_Id()
    {
        Assert.That(async () => await _service.ResolveIngredientsAsync(
                [new IngredientReference(Guid.NewGuid(), null, "not-a-guid", 1m)],
                _userId,
                CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public async Task ResolveIngredientsAsync_Should_Resolve_External_Product()
    {
        SetupProduct(CreateProduct("product-1", CreateNutritionServing("serving-1")));

        var result = await _service.ResolveIngredientsAsync(
            [new IngredientReference(null, "product-1", "serving-1", 2m)],
            _userId,
            CancellationToken.None);

        var identity = _nutritionIdentities.Single();
        var servingIdentity = identity.Servings.Single();
        Assert.That(result.Single(), Is.EqualTo(new ResolvedIngredient(
            identity.Id,
            servingIdentity.Id,
            2m,
            100m,
            10m,
            5m,
            2m)));
        _nutritionCatalogMock.Verify(
            catalog => catalog.GetAsync("product-1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public void ResolveIngredientsAsync_Should_Reject_Missing_External_Product()
    {
        _nutritionCatalogMock
            .Setup(catalog => catalog.GetAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NutritionProduct)null);

        Assert.That(async () => await _service.ResolveIngredientsAsync(
                [new IngredientReference(null, "missing", "serving-1", 1m)],
                _userId,
                CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void ResolveIngredientsAsync_Should_Reject_Missing_External_Serving()
    {
        SetupProduct(CreateProduct("product-1", CreateNutritionServing("other-serving")));

        Assert.That(async () => await _service.ResolveIngredientsAsync(
                [new IngredientReference(null, "product-1", "missing", 1m)],
                _userId,
                CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public void ResolveIngredientsAsync_Should_Require_Exactly_One_Identifier()
    {
        Assert.Multiple(() =>
        {
            Assert.That(async () => await _service.ResolveIngredientsAsync(
                    [new IngredientReference(null, null, "serving-1", 1m)],
                    _userId,
                    CancellationToken.None),
                Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
            Assert.That(async () => await _service.ResolveIngredientsAsync(
                    [new IngredientReference(Guid.NewGuid(), "product-1", "serving-1", 1m)],
                    _userId,
                    CancellationToken.None),
                Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
        });
    }

    [Test]
    public async Task ResolveIngredientsAsync_Should_Reuse_Existing_Identities_Across_Sequential_Requests()
    {
        SetupProduct(CreateProduct("product-1", CreateNutritionServing("serving-1")));
        var reference = new IngredientReference(null, "product-1", "serving-1", 1m);

        var first = (await _service.ResolveIngredientsAsync([reference], _userId, CancellationToken.None)).Single();
        var second = (await _service.ResolveIngredientsAsync([reference], _userId, CancellationToken.None)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(second.IngredientId, Is.EqualTo(first.IngredientId));
            Assert.That(second.ServingId, Is.EqualTo(first.ServingId));
        });
        _nutritionIngredientsRepositoryMock.Verify(
            repository => repository.AddManyAsync(It.IsAny<IEnumerable<NutritionIngredient>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task ResolveIngredientsAsync_Should_Preserve_Input_Order()
    {
        SetupProduct(
            CreateProduct("product-1", CreateNutritionServing("serving-1")),
            CreateProduct("product-2", CreateNutritionServing("serving-2")));

        var result = await _service.ResolveIngredientsAsync(
            [
                new IngredientReference(null, "product-2", "serving-2", 2m),
                new IngredientReference(null, "product-1", "serving-1", 1m),
            ],
            _userId,
            CancellationToken.None);

        Assert.That(result.Select(ingredient => ingredient.Quantity), Is.EqualTo(new[] { 2m, 1m }));
    }

    [Test]
    public async Task ResolveIngredientsAsync_Should_Create_Only_Identity_Data()
    {
        SetupProduct(CreateProduct("product-1", CreateNutritionServing("serving-1")));

        await _service.ResolveIngredientsAsync(
            [new IngredientReference(null, "product-1", "serving-1", 1m)],
            _userId,
            CancellationToken.None);

        var identity = _nutritionIdentities.Single();
        Assert.Multiple(() =>
        {
            Assert.That(identity.Source, Is.EqualTo("fatsecret"));
            Assert.That(identity.ProductId, Is.EqualTo("product-1"));
            Assert.That(identity.Servings.Single().ServingId, Is.EqualTo("serving-1"));
        });
    }

    [Test]
    public void ResolveIngredientsAsync_Should_Not_Write_Identities_When_Any_Reference_Is_Invalid()
    {
        SetupProduct(
            CreateProduct("product-1", CreateNutritionServing("serving-1")),
            CreateProduct("product-2", CreateNutritionServing("other-serving")));

        Assert.That(async () => await _service.ResolveIngredientsAsync(
                [
                    new IngredientReference(null, "product-1", "serving-1", 1m),
                    new IngredientReference(null, "product-2", "missing", 1m),
                ],
                _userId,
                CancellationToken.None),
            Throws.InstanceOf<BadRequestException>());
        _nutritionIngredientsRepositoryMock.Verify(
            repository => repository.AddManyAsync(It.IsAny<IEnumerable<NutritionIngredient>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _nutritionIngredientsRepositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<NutritionIngredient>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Return_Lookup_When_Ingredients_Are_Valid()
    {
        var ingredientId = Guid.NewGuid();
        var servingId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(servingId, ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        var result = await _service.GetValidatedIngredientsAsync(
            [new RequestIngredient(ingredientId, servingId, 2m)], _userId, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result.Keys, Does.Contain(ingredientId));
            Assert.That(result[ingredientId].Servings.Single().Id, Is.EqualTo(servingId));
        });
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Throw_BadRequest_When_Ingredient_Not_Found()
    {
        SetupIngredients(new List<Ingredient>());

        Assert.That(async () => await _service.GetValidatedIngredientsAsync(
                [new RequestIngredient(Guid.NewGuid(), Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Throw_BadRequest_When_Serving_Not_Found_On_Ingredient()
    {
        var ingredientId = Guid.NewGuid();
        var ingredient = CreateIngredient(ingredientId);
        ingredient.Servings.Add(CreateServing(Guid.NewGuid(), ingredientId));
        SetupIngredients(new List<Ingredient> { ingredient });

        Assert.That(async () => await _service.GetValidatedIngredientsAsync(
                [new RequestIngredient(ingredientId, Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    [Test]
    public async Task GetValidatedIngredientsAsync_Should_Throw_BadRequest_When_Ingredient_Is_Not_Owned_By_User()
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

        Assert.That(async () => await _service.GetValidatedIngredientsAsync(
                [new RequestIngredient(ingredientId, Guid.NewGuid(), 1m)], _userId, CancellationToken.None),
            Throws.InstanceOf<BadRequestException>().With.Message.EqualTo("INGREDIENT_ID_INVALID"));
    }

    private static NutritionProduct CreateProduct(string productId, params CgmLink.Nutrition.Source.NutritionServing[] servings) => new()
    {
        ProductId = productId,
        Name = "Product",
        Servings = servings,
    };

    private static CgmLink.Nutrition.Source.NutritionServing CreateNutritionServing(string servingId) => new()
    {
        ExternalId = servingId,
        Calories = 100m,
        Carbs = 10m,
        Protein = 5m,
        Fat = 2m,
    };

    private void SetupProduct(params NutritionProduct[] products)
    {
        foreach (var product in products)
        {
            _nutritionCatalogMock
                .Setup(catalog => catalog.GetAsync(product.ProductId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
        }
    }

    [Test]
    public async Task DetailLookup_OwnedIngredient_DoesNotCallProvider()
    {
        var ingredient = CreateIngredient(Guid.NewGuid());
        _ingredientsRepositoryMock.Setup(repository => repository.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(new[] { ingredient }));
        _nutritionCatalogMock.Setup(catalog => catalog.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException());

        var response = await _service.GetIngredientAsync(ingredient.Id.ToString(), _userId);

        Assert.That(response.IngredientId, Is.EqualTo(ingredient.Id));
        _nutritionCatalogMock.Verify(catalog => catalog.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase("123")]
    [TestCase("ccda0e86-48bb-47c1-ad0a-6c857bd77549")]
    public async Task DetailLookup_ProductIdentifier_ReturnsHydratedProductWithoutCreatingIdentities(string identifier)
    {
        _ingredientsRepositoryMock.Setup(repository => repository.GetAll(It.IsAny<FindOptions>()))
            .Returns(new TestAsyncEnumerable<Ingredient>(Array.Empty<Ingredient>()));
        var timestamp = DateTimeOffset.UtcNow.AddHours(-1);
        SetupProduct(new NutritionProduct
        {
            ProductId = identifier,
            Name = "Provider milk",
            DataAsOf = timestamp,
            Attribution = "Provider attribution can change",
            Servings = [CreateNutritionServing("456")],
        });

        var response = await _service.GetIngredientAsync(identifier, _userId);

        Assert.Multiple(() =>
        {
            Assert.That(response.IngredientId, Is.Null);
            Assert.That(response.ProductId, Is.EqualTo(identifier));
            Assert.That(response.Name, Is.EqualTo("Provider milk"));
            Assert.That(response.Servings.Single().ServingId, Is.EqualTo("456"));
            Assert.That(response.DataAsOf, Is.EqualTo(timestamp));
            Assert.That(response.Attribution, Is.EqualTo("Provider attribution can change"));
            Assert.That(_nutritionIdentities, Is.Empty);
        });
        _nutritionIngredientsRepositoryMock.Verify(repository => repository.GetAll(It.IsAny<FindOptions>()), Times.Never);
    }

    [Test]
    public void DetailLookup_ProviderFailure_IsMappedToServiceUnavailable()
    {
        _nutritionCatalogMock.Setup(catalog => catalog.GetAsync("123", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException("private provider detail"));

        Assert.That(async () => await _service.GetIngredientAsync("123", _userId),
            Throws.TypeOf<ApiException>().With.Property("StatusCode").EqualTo(System.Net.HttpStatusCode.ServiceUnavailable)
                .And.Message.EqualTo("NUTRITION_UNAVAILABLE"));
    }

    [Test]
    public void DetailLookup_RequestCancellation_IsPreserved()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        _nutritionCatalogMock.Setup(catalog => catalog.GetAsync("123", cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));

        Assert.That(async () => await _service.GetIngredientAsync("123", _userId, cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    private sealed record RequestIngredient(Guid IngredientId, Guid ServingId, decimal Quantity) : IMealIngredientRequest;
}
