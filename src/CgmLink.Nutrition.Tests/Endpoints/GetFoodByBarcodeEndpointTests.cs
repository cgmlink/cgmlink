using CgmLink.Nutrition.Endpoints.GetFood;
using CgmLink.Nutrition.Source;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using BarcodeEndpoint = CgmLink.Nutrition.Endpoints.GetFoodByBarcode.Endpoint;

namespace CgmLink.Nutrition.Tests.Endpoints;

[TestFixture]
public class GetFoodByBarcodeEndpointTests
{
    private Mock<INutritionCatalog> _catalogMock;

    [SetUp]
    public void SetUp()
    {
        _catalogMock = new Mock<INutritionCatalog>();
    }

    [Test]
    public async Task HandleAsync_Should_Use_Barcode_Lookup()
    {
        _catalogMock
            .Setup(client => client.GetByBarcodeAsync("0000012345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NutritionProduct
            {
                ProductId = "50953",
                Name = "Cereal",
                Barcode = "0000012345678",
            });

        var result = await BarcodeEndpoint.HandleAsync(
            "0000012345678", _catalogMock.Object, CancellationToken.None);

        var response = (result.Result as Ok<GetFoodResponse>)!.Value;
        Assert.Multiple(() =>
        {
            Assert.That(response.ProductId, Is.EqualTo("50953"));
            Assert.That(response.Barcode, Is.EqualTo("0000012345678"));
        });
        _catalogMock.Verify(client => client.GetByBarcodeAsync(
            "0000012345678", It.IsAny<CancellationToken>()), Times.Once);
    }
}
