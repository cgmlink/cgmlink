using CgmLink.Nutrition.FatSecretClient;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class FatSecretClientTests
{
    private static readonly FatSecretOptions OptionsValue = new()
    {
        ClientId = "client-id",
        ClientSecret = "secret",
        Region = "GB",
        Language = "en"
    };

    [Test]
    public async Task GetAsync_ReturnsOnlyStoredProductAndServingData()
    {
        const string json = """
            {
              "food": {
                "food_id": "50953",
                "food_name": "Whole Grain Cheerios",
                "brand_name": "General Mills",
                "food_url": "https://example.test/ignored",
                "servings": {
                  "serving": [{
                    "serving_id": "100675",
                    "serving_description": "1 cup",
                    "metric_serving_amount": "30.000",
                    "metric_serving_unit": "g",
                    "calories": "100",
                    "carbohydrate": "20.00",
                    "protein": "3.00",
                    "fat": "2.00",
                    "sodium": "160"
                  }]
                }
              }
            }
            """;
        var handler = JsonHandler(json);
        var sut = CreateClient(handler);

        var result = await sut.GetAsync("50953");

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.ProductId, Is.EqualTo("50953"));
            Assert.That(result.Name, Is.EqualTo("Whole Grain Cheerios"));
            Assert.That(result.Servings, Has.Count.EqualTo(1));
            Assert.That(result.Servings.Single().ExternalId, Is.EqualTo("100675"));
            Assert.That(result.Servings.Single().ServingAmount, Is.EqualTo(30m));
            Assert.That(result.Servings.Single().ServingUnit, Is.EqualTo("g"));
            Assert.That(result.Servings.Single().Calories, Is.EqualTo(100m));
            Assert.That(result.Servings.Single().Carbs, Is.EqualTo(20m));
            Assert.That(result.Servings.Single().Protein, Is.EqualTo(3m));
            Assert.That(result.Servings.Single().Fat, Is.EqualTo(2m));
            Assert.That(handler.LastRequest!.RequestUri!.PathAndQuery,
                Is.EqualTo("/rest/food/v5?food_id=50953&format=json&region=GB&language=en"));
            Assert.That(handler.LastRequest.Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
            Assert.That(handler.LastRequest.Headers.Authorization.Parameter, Is.EqualTo("access-token"));
        });
    }

    [Test]
    public async Task SearchAsync_ReturnsMappedFoodsAndUsesPaging()
    {
        const string json = """
            {"foods_search":{"results":{"food":[
              {"food_id":"1","food_name":"Apple","servings":{"serving":[]}},
              {"food_id":"2","food_name":"Apple Pie","servings":{"serving":[]}}
            ]}}}
            """;
        var handler = JsonHandler(json);
        var sut = CreateClient(handler);

        var result = await sut.SearchAsync("apple pie", 2, 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Select(food => food.ProductId), Is.EqualTo(new[] { "1", "2" }));
            Assert.That(handler.LastRequest!.RequestUri!.PathAndQuery, Is.EqualTo(
                "/rest/foods/search/v5?search_expression=apple%20pie&page_number=2&max_results=10&format=json&region=GB&language=en"));
        });
    }

    [Test]
    public async Task GetByBarcodeAsync_UsesBarcodeEndpoint()
    {
        var handler = JsonHandler("""{"food":{"food_id":"50953","food_name":"Cereal"}}""");
        var sut = CreateClient(handler);

        var result = await sut.GetByBarcodeAsync("0000012345678");

        Assert.Multiple(() =>
        {
            Assert.That(result!.ProductId, Is.EqualTo("50953"));
            Assert.That(handler.LastRequest!.RequestUri!.PathAndQuery, Is.EqualTo(
                "/rest/food/barcode/find-by-id/v2?barcode=0000012345678&format=json&region=GB&language=en"));
        });
    }

    private static CgmLink.Nutrition.FatSecretClient.FatSecretClient CreateClient(RecordingHandler handler)
    {
        var authenticator = new Mock<IFatSecretAuthenticator>();
        authenticator.Setup(value => value.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");
        return new CgmLink.Nutrition.FatSecretClient.FatSecretClient(
            new HttpClient(handler) { BaseAddress = new Uri("https://platform.fatsecret.com/rest/") },
            authenticator.Object,
            Options.Create(OptionsValue));
    }

    private static RecordingHandler JsonHandler(string json) => new(_ => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    });
}
