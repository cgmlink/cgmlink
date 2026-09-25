using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Nutrition.FatSecretClient;
using CgmLink.Nutrition.FatSecretClient.Exceptions;
using Microsoft.Extensions.Options;
using FatSecretClientImplementation = CgmLink.Nutrition.FatSecretClient.FatSecretClient;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class FatSecretClientTests
{
    [Test]
    public async Task SearchAsync_UsesOAuth2BearerRequestAndReturnsNormalizedItems()
    {
        var handler = new RecordingHandler((uri, _) =>
        {
            if (uri.AbsolutePath == "/connect/token")
            {
                return JsonResponse("""
                    {"access_token":"access-token","token_type":"Bearer","expires_in":3600}
                    """);
            }

            return JsonResponse("""
                {
                  "foods": {
                    "food": [{
                      "food_id": "33678",
                      "food_name": "Apple",
                      "brand_name": "Ignored",
                      "food_description": "Ignored"
                    }],
                    "max_results": "20",
                    "page_number": "0",
                    "total_results": "1"
                  }
                }
                """);
        });
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var tokenProvider = CreateTokenProvider(handler, options);
        var client = new FatSecretClientImplementation(httpClient, tokenProvider, options);

        var result = await client.SearchAsync("apple", pageNumber: 0, maxResults: 20);

        Assert.Multiple(() =>
        {
            Assert.That(result.Items, Has.Count.EqualTo(1));
            Assert.That(result.Items[0].ProductId, Is.EqualTo("33678"));
            Assert.That(result.Items[0].Name, Is.EqualTo("Apple"));
            Assert.That(result.TotalResults, Is.EqualTo(1));
            Assert.That(result.PageNumber, Is.EqualTo(0));
            Assert.That(result.MaxResults, Is.EqualTo(20));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
        });

        var tokenRequest = handler.Requests[0];
        var apiRequest = handler.Requests[1];
        var query = ParseQuery(apiRequest.Uri);
        var form = ParseForm(tokenRequest.Body);

        Assert.Multiple(() =>
        {
            Assert.That(tokenRequest.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(tokenRequest.Authorization, Is.EqualTo("Basic " + Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes("client-id:client-secret"))));
            Assert.That(form["grant_type"], Is.EqualTo("client_credentials"));
            Assert.That(form["scope"], Is.EqualTo("basic"));
            Assert.That(apiRequest.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(apiRequest.Authorization, Is.EqualTo("Bearer access-token"));
            Assert.That(apiRequest.ContentType, Does.StartWith("application/json"));
            Assert.That(apiRequest.Body, Is.Empty);
            Assert.That(query["method"], Is.EqualTo("foods.search"));
            Assert.That(query["search_expression"], Is.EqualTo("apple"));
            Assert.That(query["page_number"], Is.EqualTo("0"));
            Assert.That(query["max_results"], Is.EqualTo("20"));
            Assert.That(query["format"], Is.EqualTo("json"));
            Assert.That(query["region"], Is.EqualTo("US"));
            Assert.That(query["language"], Is.EqualTo("en"));
        });
    }

    [Test]
    public async Task GetFoodAsync_MapsOnlyNormalizedFoodAndServingFields()
    {
        var handler = new RecordingHandler((uri, _) =>
        {
            if (uri.AbsolutePath == "/connect/token")
            {
                return JsonResponse("""
                    {"access_token":"access-token","expires_in":3600}
                    """);
            }

            return JsonResponse("""
                {
                  "food": {
                    "food_id": "33678",
                    "food_name": "Apple",
                    "brand_name": "Ignored",
                    "servings": {
                      "serving": {
                        "serving_id": "323744",
                        "serving_description": "100 g",
                        "serving_url": "Ignored",
                        "metric_serving_amount": "100.000",
                        "metric_serving_unit": "g",
                        "number_of_units": "1.000",
                        "measurement_description": "Ignored",
                        "calories": "52",
                        "carbohydrate": "13.81",
                        "protein": "0.26",
                        "fat": "0.17"
                      }
                    }
                  }
                }
                """);
        });
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions("https://platform.fatsecret.com/rest/server.api"));
        var tokenProvider = CreateTokenProvider(handler, options);
        var client = new FatSecretClientImplementation(httpClient, tokenProvider, options);

        var food = await client.GetFoodAsync("33678");

        Assert.That(food, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(food!.ProductId, Is.EqualTo("33678"));
            Assert.That(food.Name, Is.EqualTo("Apple"));
            Assert.That(food.Servings, Has.Count.EqualTo(1));
            Assert.That(food.Servings[0].ExternalId, Is.EqualTo("323744"));
            Assert.That(food.Servings[0].Description, Is.EqualTo("100 g"));
            Assert.That(food.Servings[0].ServingAmount, Is.EqualTo(100m));
            Assert.That(food.Servings[0].ServingUnit, Is.EqualTo("g"));
            Assert.That(food.Servings[0].Calories, Is.EqualTo(52m));
            Assert.That(food.Servings[0].Carbs, Is.EqualTo(13.81m));
            Assert.That(food.Servings[0].Protein, Is.EqualTo(0.26m));
            Assert.That(food.Servings[0].Fat, Is.EqualTo(0.17m));
        });

        var query = ParseQuery(handler.Requests[1].Uri);
        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests[1].Uri.AbsolutePath, Is.EqualTo("/rest/server.api"));
            Assert.That(query["method"], Is.EqualTo("food.get"));
            Assert.That(query["food_id"], Is.EqualTo("33678"));
            Assert.That(query["region"], Is.EqualTo("US"));
            Assert.That(query["language"], Is.EqualTo("en"));
        });
    }

    [Test]
    public void GetFoodAsync_MissingRequiredMacro_ThrowsFatSecretApiException()
    {
        var handler = new RecordingHandler((uri, _) => uri.AbsolutePath == "/connect/token"
            ? JsonResponse("{\"access_token\":\"access-token\",\"expires_in\":3600}")
            : JsonResponse("""
                {
                  "food": {
                    "food_id": "33678",
                    "food_name": "Apple",
                    "servings": {
                      "serving": {
                        "calories": "52",
                        "carbohydrate": "13.81",
                        "protein": "0.26"
                      }
                    }
                  }
                }
                """));
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var client = new FatSecretClientImplementation(
            httpClient,
            CreateTokenProvider(handler, options),
            options);

        var exception = Assert.ThrowsAsync<FatSecretApiException>(async () => await client.GetFoodAsync("33678"));

        Assert.That(exception!.Message, Does.Contain("'fat'"));
    }

    [Test]
    public void SearchAsync_MissingProductId_ThrowsFatSecretApiException()
    {
        var handler = new RecordingHandler((uri, _) => uri.AbsolutePath == "/connect/token"
            ? JsonResponse("{\"access_token\":\"access-token\",\"expires_in\":3600}")
            : JsonResponse("{\"foods\":{\"food\":{\"food_name\":\"Apple\"}}}"));
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var client = new FatSecretClientImplementation(
            httpClient,
            CreateTokenProvider(handler, options),
            options);

        var exception = Assert.ThrowsAsync<FatSecretApiException>(async () => await client.SearchAsync("apple"));

        Assert.That(exception!.Message, Does.Contain("'food_id'"));
    }

    [Test]
    public async Task AccessTokenProvider_CachesTokenUntilExpiry()
    {
        var handler = new RecordingHandler((_, _) => JsonResponse("""
            {"access_token":"cached-token","expires_in":3600}
            """));
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var provider = CreateTokenProvider(handler, options);

        var firstToken = await provider.GetAccessTokenAsync();
        var secondToken = await provider.GetAccessTokenAsync();

        Assert.Multiple(() =>
        {
            Assert.That(firstToken, Is.EqualTo("cached-token"));
            Assert.That(secondToken, Is.EqualTo("cached-token"));
            Assert.That(handler.Requests, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task AccessTokenProvider_InvalidateIfCurrent_PreservesNewerToken()
    {
        var handler = new RecordingHandler((_, index) => JsonResponse(
            $"{{\"access_token\":\"token-{index}\",\"expires_in\":3600}}"));
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var provider = CreateTokenProvider(handler, options);

        var firstToken = await provider.GetAccessTokenAsync();
        provider.InvalidateIfCurrent("different-token");
        var cachedToken = await provider.GetAccessTokenAsync();
        provider.InvalidateIfCurrent(firstToken);
        var refreshedToken = await provider.GetAccessTokenAsync();

        Assert.Multiple(() =>
        {
            Assert.That(cachedToken, Is.EqualTo(firstToken));
            Assert.That(refreshedToken, Is.EqualTo("token-1"));
            Assert.That(handler.Requests, Has.Count.EqualTo(2));
        });
    }

    [TestCase(HttpStatusCode.Unauthorized)]
    [TestCase(HttpStatusCode.OK)]
    public async Task SearchAsync_RefreshesTokenAfterAuthenticationError(HttpStatusCode statusCode)
    {
        var handler = new RecordingHandler((uri, index) =>
        {
            if (uri.AbsolutePath == "/connect/token")
            {
                return JsonResponse(index == 0
                    ? """
                      {"access_token":"first-token","expires_in":3600}
                      """
                    : """
                      {"access_token":"second-token","expires_in":3600}
                      """);
            }

            return index == 1
                ? JsonResponse("{\"error\":{\"code\":13,\"message\":\"Invalid token\"}}", statusCode)
                : JsonResponse("{\"foods\":{\"food\":[],\"total_results\":\"0\"}}");
        });
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var tokenProvider = CreateTokenProvider(handler, options);
        var client = new FatSecretClientImplementation(httpClient, tokenProvider, options);

        await client.SearchAsync("apple");

        Assert.Multiple(() =>
        {
            Assert.That(handler.Requests, Has.Count.EqualTo(4));
            Assert.That(handler.Requests[1].Authorization, Is.EqualTo("Bearer first-token"));
            Assert.That(handler.Requests[3].Authorization, Is.EqualTo("Bearer second-token"));
        });
    }

    [Test]
    public void SearchAsync_ApiError_ThrowsFatSecretApiException()
    {
        var handler = new RecordingHandler((uri, _) =>
        {
            if (uri.AbsolutePath == "/connect/token")
            {
                return JsonResponse("{\"access_token\":\"access-token\",\"expires_in\":3600}");
            }

            return JsonResponse("""
                {"error":{"code":"JSON_PARSE_ERROR","message":"Invalid request."}}
                """);
        });
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var tokenProvider = CreateTokenProvider(handler, options);
        var client = new FatSecretClientImplementation(httpClient, tokenProvider, options);

        var exception = Assert.ThrowsAsync<FatSecretApiException>(async () => await client.SearchAsync("apple"));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Is.EqualTo("Invalid request."));
            Assert.That(exception.ErrorCode, Is.EqualTo("JSON_PARSE_ERROR"));
        });
    }

    [Test]
    public async Task AccessTokenProvider_ScalarOAuthError_PreservesCodeAndDescription()
    {
        var handler = new RecordingHandler((_, _) => JsonResponse(
            "{\"error\":\"invalid_client\",\"error_description\":\"Client authentication failed.\"}",
            HttpStatusCode.BadRequest));
        using var httpClient = CreateHttpClient(handler);
        var provider = CreateTokenProvider(handler, Options.Create(CreateOptions()));

        var exception = Assert.ThrowsAsync<FatSecretApiException>(async () => await provider.GetAccessTokenAsync());

        Assert.Multiple(() =>
        {
            Assert.That(exception!.StatusCode, Is.EqualTo(400));
            Assert.That(exception.ErrorCode, Is.EqualTo("invalid_client"));
            Assert.That(exception.Message, Is.EqualTo("Client authentication failed."));
        });
    }

    [Test]
    public void SearchAsync_BlankExpression_Throws()
    {
        var handler = new RecordingHandler((_, _) => JsonResponse("{}"));
        using var httpClient = CreateHttpClient(handler);
        var options = Options.Create(CreateOptions());
        var tokenProvider = CreateTokenProvider(handler, options);
        var client = new FatSecretClientImplementation(httpClient, tokenProvider, options);

        Assert.Multiple(() =>
        {
            Assert.That(async () => await client.SearchAsync(""), Throws.TypeOf<ArgumentException>());
            Assert.That(async () => await client.SearchAsync(" "), Throws.TypeOf<ArgumentException>());
        });
    }

    private static FatSecretOptions CreateOptions(
        string apiBaseUrl = "https://platform.fatsecret.com/rest/")
    {
        return new FatSecretOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scope = "basic",
            ApiBaseUrl = apiBaseUrl,
        };
    }

    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("https://platform.fatsecret.com/rest/"),
        };
    }

    private static FatSecretAccessTokenProvider CreateTokenProvider(
        HttpMessageHandler handler,
        IOptions<FatSecretOptions> options)
    {
        return new FatSecretAccessTokenProvider(new StubHttpClientFactory(handler), options);
    }

    private static HttpResponseMessage JsonResponse(
        string body,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body),
        };
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(Uri uri)
    {
        return ParseForm(uri.Query);
    }

    private static IReadOnlyDictionary<string, string> ParseForm(string value)
    {
        return value
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(
                pair => Uri.UnescapeDataString(pair[0]),
                pair => Uri.UnescapeDataString(pair.Length > 1 ? pair[1] : ""));
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<Uri, int, HttpResponseMessage> _responder;

        public RecordingHandler(Func<Uri, int, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public List<RequestSnapshot> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RequestSnapshot(
                request.RequestUri!,
                request.Method,
                request.Headers.Authorization?.ToString(),
                request.Content?.Headers.ContentType?.ToString(),
                body));
            return _responder(request.RequestUri!, Requests.Count - 1);
        }
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false)
        {
            BaseAddress = new Uri("https://platform.fatsecret.com/rest/"),
        };
    }

    private sealed record RequestSnapshot(
        Uri Uri,
        HttpMethod Method,
        string Authorization,
        string ContentType,
        string Body);
}
