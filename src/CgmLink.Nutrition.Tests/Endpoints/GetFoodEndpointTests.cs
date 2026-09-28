using CgmLink.AspNetCore.Exceptions;
using CgmLink.Nutrition.Endpoints.GetFood;
using CgmLink.Nutrition.Source;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CgmLink.Nutrition.Tests.Endpoints;

[TestFixture]
public class GetFoodEndpointTests
{
    private Mock<INutritionCatalog> _catalogMock;

    [SetUp]
    public void SetUp()
    {
        _catalogMock = new Mock<INutritionCatalog>();
    }

    [Test]
    public async Task HandleAsync_Should_Return_Mapped_Food()
    {
        _catalogMock.Setup(client => client.GetAsync("50953", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NutritionProduct
            {
                ProductId = "50953",
                Name = "Cereal",
                Servings =
                [
                    new NutritionServing
                    {
                        ExternalId = "100675",
                        Description = "1 cup",
                        Calories = 100,
                        Carbs = 20,
                        Protein = 3,
                        Fat = 2,
                    }
                ],
            });

        var result = await Endpoint.HandleAsync(
            "50953", _catalogMock.Object, CancellationToken.None);

        var response = (result.Result as Ok<GetFoodResponse>)!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(response.ProductId, Is.EqualTo("50953"));
            Assert.That(response.Name, Is.EqualTo("Cereal"));
            Assert.That(response.Servings.Single().ExternalId, Is.EqualTo("100675"));
        });
    }

    [Test]
    public void HandleAsync_Should_Throw_NotFoundException_When_Food_Does_Not_Exist()
    {
        _catalogMock.Setup(client => client.GetAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NutritionProduct)null);

        Assert.That(
            async () => await Endpoint.HandleAsync(
                "missing", _catalogMock.Object, CancellationToken.None),
            Throws.InstanceOf<NotFoundException>().With.Message.EqualTo("FOOD_NOT_FOUND"));
    }
}
