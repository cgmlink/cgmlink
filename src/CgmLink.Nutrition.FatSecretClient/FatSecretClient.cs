using CgmLink.Nutrition.Source;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.FatSecretClient;

internal sealed class FatSecretClient : INutritionSourceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private readonly HttpClient _httpClient;
    private readonly IFatSecretAuthenticator _authenticator;

    public FatSecretClient(
        HttpClient httpClient,
        IFatSecretAuthenticator authenticator)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
    }

    public string Source => "fatsecret";

    public async Task<IReadOnlyCollection<NutritionProduct>> SearchAsync(
        string searchExpression,
        int pageNumber = 0,
        int maxResults = 20,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchExpression);
        ArgumentOutOfRangeException.ThrowIfNegative(pageNumber);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxResults, 50);

        using var request = new HttpRequestMessage(HttpMethod.Post, "server.api")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["method"] = "foods.search",
                ["search_expression"] = searchExpression,
                ["page_number"] = pageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["max_results"] = maxResults.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["format"] = "json"
            })
        };
        var response = await SendAsync<SearchResponse>(request, cancellationToken).ConfigureAwait(false);

        return response?.Foods is null ? [] : MapSearchResults(response.Foods.Food);
    }

    public async Task<NutritionProduct?> GetAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);

        var response = await GetAsync<FoodResponse>(
            BuildPath("food/v5", new Dictionary<string, string> { ["food_id"] = productId }),
            cancellationToken).ConfigureAwait(false);

        return response?.Food is null ? null : Map(response.Food);
    }

    public async Task<NutritionProduct?> GetByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        var response = await GetAsync<FoodResponse>(
            BuildPath("food/barcode/find-by-id/v2", new Dictionary<string, string> { ["barcode"] = barcode }),
            cancellationToken).ConfigureAwait(false);

        return response?.Food is null ? null : Map(response.Food);
    }

    private async Task<T?> GetAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        return await SendAsync<T>(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await _authenticator.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false));

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var error = JsonSerializer.Deserialize<ErrorResponse>(json, JsonOptions)?.Error;
        if (error is not null)
        {
            throw new HttpRequestException($"FatSecret API error {error.Code}: {error.Message}");
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private string BuildPath(string path, IReadOnlyDictionary<string, string> parameters)
    {
        var allParameters = new Dictionary<string, string>(parameters)
        {
            ["format"] = "json",
        };

        return $"{path}?{string.Join("&", allParameters.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";
    }

    private static IReadOnlyCollection<NutritionProduct> MapSearchResults(JsonElement food)
    {
        if (food.ValueKind == JsonValueKind.Array)
        {
            return food.EnumerateArray()
                .Select(item => Map(item.Deserialize<FoodDto>(JsonOptions)!))
                .ToArray();
        }

        return food.ValueKind == JsonValueKind.Object
            ? [Map(food.Deserialize<FoodDto>(JsonOptions)!)]
            : [];
    }

    private static NutritionProduct Map(FoodDto food) => new()
    {
        ProductId = food.ProductId,
        Name = food.Name,
        Servings = food.Servings?.Items.Select(serving => new NutritionServing
        {
            ExternalId = serving.ExternalId,
            Description = serving.Description,
            ServingAmount = serving.ServingAmount,
            ServingUnit = serving.ServingUnit,
            Calories = serving.Calories,
            Carbs = serving.Carbs,
            Protein = serving.Protein,
            Fat = serving.Fat
        }).ToArray() ?? []
    };

    private sealed class SearchResponse
    {
        [JsonPropertyName("foods")]
        public FoodsDto? Foods { get; init; }
    }

    private sealed class FoodsDto
    {
        [JsonPropertyName("food")]
        public JsonElement Food { get; init; }
    }

    private sealed class FoodResponse
    {
        [JsonPropertyName("food")]
        public FoodDto? Food { get; init; }
    }

    private sealed class FoodDto
    {
        [JsonPropertyName("food_id")]
        public string ProductId { get; init; } = "";

        [JsonPropertyName("food_name")]
        public string Name { get; init; } = "";

        [JsonPropertyName("servings")]
        public ServingsDto? Servings { get; init; }
    }

    private sealed class ServingsDto
    {
        [JsonPropertyName("serving")]
        public ServingDto[] Items { get; init; } = [];
    }

    private sealed class ServingDto
    {
        [JsonPropertyName("serving_id")]
        public string ExternalId { get; init; } = "";

        [JsonPropertyName("serving_description")]
        public string? Description { get; init; }

        [JsonPropertyName("metric_serving_amount")]
        public decimal? ServingAmount { get; init; }

        [JsonPropertyName("metric_serving_unit")]
        public string? ServingUnit { get; init; }

        [JsonPropertyName("calories")]
        public decimal Calories { get; init; }

        [JsonPropertyName("carbohydrate")]
        public decimal Carbs { get; init; }

        [JsonPropertyName("protein")]
        public decimal Protein { get; init; }

        [JsonPropertyName("fat")]
        public decimal Fat { get; init; }
    }

    private sealed class ErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDto? Error { get; init; }
    }

    private sealed class ErrorDto
    {
        [JsonPropertyName("code")]
        public string Code { get; init; } = "";

        [JsonPropertyName("message")]
        public string Message { get; init; } = "";
    }
}
