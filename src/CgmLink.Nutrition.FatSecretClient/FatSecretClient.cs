using CgmLink.Nutrition.FatSecretClient.Models;
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

        var formValues = new Dictionary<string, string>
        {
            ["method"] = "foods.search",
            ["search_expression"] = searchExpression,
            ["page_number"] = pageNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["max_results"] = maxResults.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["format"] = "json"
        };
        var response = await SendAsync<SearchResponse>(() => new HttpRequestMessage(HttpMethod.Post, "server.api")
        {
            Content = new FormUrlEncodedContent(formValues)
        }, cancellationToken).ConfigureAwait(false);

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
        return await SendAsync<T>(
            () => new HttpRequestMessage(HttpMethod.Get, path),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<T?> SendAsync<T>(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken)
    {
        var accessToken = await _authenticator.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        using var response = await SendOnceAsync(requestFactory, accessToken, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var error = DeserializeError(json);

        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized && error?.Code != 13)
        {
            return Deserialize<T>(response, json, error);
        }

        await _authenticator.InvalidateAccessTokenAsync(accessToken, cancellationToken).ConfigureAwait(false);
        accessToken = await _authenticator.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        using var retryResponse = await SendOnceAsync(requestFactory, accessToken, cancellationToken).ConfigureAwait(false);
        var retryJson = await retryResponse.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var retryError = DeserializeError(retryJson);
        return Deserialize<T>(retryResponse, retryJson, retryError);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        Func<HttpRequestMessage> requestFactory,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = requestFactory();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static T? Deserialize<T>(HttpResponseMessage response, string json, Error? error)
    {
        response.EnsureSuccessStatusCode();

        if (error is not null)
        {
            throw new HttpRequestException($"FatSecret API error {error.Code}: {error.Message}");
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static Error? DeserializeError(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ErrorResponse>(json, JsonOptions)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
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
                .Select(item => Map(item.Deserialize<Food>(JsonOptions)!))
                .ToArray();
        }

        return food.ValueKind == JsonValueKind.Object
            ? [Map(food.Deserialize<Food>(JsonOptions)!)]
            : [];
    }

    private static NutritionProduct Map(Food food) => new()
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

}
