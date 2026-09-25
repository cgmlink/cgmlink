using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Nutrition.Contracts.Models;
using CgmLink.Nutrition.FatSecretClient.Exceptions;
using CgmLink.Nutrition.FatSecretClient.Json;
using CgmLink.Nutrition.FatSecretClient.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Nutrition.FatSecretClient;

internal sealed class FatSecretClient : IFatSecretClient
{
    public string Source => "fatsecret";

    private readonly HttpClient _httpClient;
    private readonly IFatSecretAccessTokenProvider _accessTokenProvider;
    private readonly IOptions<FatSecretOptions> _options;

    public FatSecretClient(
        HttpClient httpClient,
        IFatSecretAccessTokenProvider accessTokenProvider,
        IOptions<FatSecretOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _accessTokenProvider = accessTokenProvider ?? throw new ArgumentNullException(nameof(accessTokenProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<NutritionSearchResults> SearchAsync(
        string searchExpression,
        int? pageNumber = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchExpression);
        if (pageNumber < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageNumber));
        }

        if (maxResults is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults));
        }

        var options = _options.Value;
        var parameters = new Dictionary<string, string>
        {
            ["method"] = "foods.search",
            ["search_expression"] = searchExpression,
            ["format"] = "json",
        };
        AddIfNotNull(parameters, "page_number", pageNumber);
        AddIfNotNull(parameters, "max_results", maxResults);
        AddIfNotBlank(parameters, "region", options.Region);
        AddIfNotBlank(parameters, "language", options.Language);

        var response = await RequestAsync<FoodSearchResponse>(parameters, cancellationToken).ConfigureAwait(false);
        var data = response?.Foods;
        var items = data?.Food.Select(MapSearchResult)
            .ToArray() ?? [];

        return new NutritionSearchResults(
            items,
            data?.TotalResults,
            data?.PageNumber,
            data?.MaxResults);
    }

    public async Task<NutritionFood?> GetFoodAsync(
        string productId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "food.get",
            ["food_id"] = productId,
            ["format"] = "json",
        };
        var options = _options.Value;
        AddIfNotBlank(parameters, "region", options.Region);
        AddIfNotBlank(parameters, "language", options.Language);

        var response = await RequestAsync<FoodGetResponse>(parameters, cancellationToken).ConfigureAwait(false);
        var food = response?.Food;
        return food is null ? null : MapFood(food);
    }

    private async Task<T?> RequestAsync<T>(
        IDictionary<string, string> parameters,
        CancellationToken cancellationToken,
        bool allowUnauthorizedRetry = true)
        where T : class
    {
        var options = _options.Value;
        var endpoint = BuildEndpointUri(options.ApiBaseUrl);
        var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri(parameters, endpoint))
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var exception = FatSecretApiException.FromResponse(content, (int)response.StatusCode);
            if (allowUnauthorizedRetry && IsAuthenticationFailure(response.StatusCode, exception))
            {
                _accessTokenProvider.InvalidateIfCurrent(accessToken);
                return await RequestAsync<T>(parameters, cancellationToken, false).ConfigureAwait(false);
            }

            throw exception;
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return default;
        }

        try
        {
            return FatSecretJsonSerializer.Deserialize<T>(
                content,
                (int)response.StatusCode,
                "FatSecret API response was not valid JSON.");
        }
        catch (FatSecretApiException exception) when (
            allowUnauthorizedRetry && IsAuthenticationFailure(response.StatusCode, exception))
        {
            _accessTokenProvider.InvalidateIfCurrent(accessToken);
            return await RequestAsync<T>(parameters, cancellationToken, false).ConfigureAwait(false);
        }
    }

    private static NutritionFoodSearchResult MapSearchResult(FoodSearchItem item)
    {
        return new NutritionFoodSearchResult
        {
            ProductId = RequiredText(item.FoodId, "food_id"),
            Name = RequiredText(item.FoodName, "food_name"),
        };
    }

    private static NutritionFood MapFood(FoodDetails food)
    {
        return new NutritionFood
        {
            ProductId = RequiredText(food.FoodId, "food_id"),
            Name = RequiredText(food.FoodName, "food_name"),
            Servings = food.Servings?.Serving.Select(MapServing).ToArray() ?? [],
        };
    }

    private static NutritionServing MapServing(FoodServingResponse serving)
    {
        return new NutritionServing
        {
            ExternalId = serving.ServingId,
            Description = serving.ServingDescription,
            ServingAmount = serving.MetricServingAmount,
            ServingUnit = serving.MetricServingUnit,
            Calories = RequiredNumber(serving.Calories, "calories"),
            Carbs = RequiredNumber(serving.Carbohydrate, "carbohydrate"),
            Protein = RequiredNumber(serving.Protein, "protein"),
            Fat = RequiredNumber(serving.Fat, "fat"),
        };
    }

    private static string RequiredText(string? value, string fieldName)
    {
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw InvalidData(fieldName);
    }

    private static decimal RequiredNumber(decimal? value, string fieldName)
    {
        return value ?? throw InvalidData(fieldName);
    }

    private static FatSecretApiException InvalidData(string fieldName)
    {
        return new FatSecretApiException($"FatSecret API response did not contain required field '{fieldName}'.");
    }

    private static bool IsAuthenticationFailure(
        HttpStatusCode statusCode,
        FatSecretApiException exception)
    {
        return statusCode == HttpStatusCode.Unauthorized
            || string.Equals(exception.ErrorCode, "13", StringComparison.Ordinal);
    }

    private static Uri BuildEndpointUri(string apiBaseUrl)
    {
        var baseUri = new Uri(apiBaseUrl, UriKind.Absolute);
        var path = baseUri.AbsolutePath.TrimEnd('/');
        if (!path.Equals("server.api", StringComparison.OrdinalIgnoreCase)
            && !path.EndsWith("/server.api", StringComparison.OrdinalIgnoreCase))
        {
            path = $"{path}/server.api";
        }

        return new UriBuilder(baseUri)
        {
            Path = path,
            Query = string.Empty,
            Fragment = string.Empty,
        }.Uri;
    }

    private static Uri BuildRequestUri(IEnumerable<KeyValuePair<string, string>> parameters, Uri endpoint)
    {
        var query = string.Join(
            "&",
            parameters.Select(parameter =>
                $"{Uri.EscapeDataString(parameter.Key)}={Uri.EscapeDataString(parameter.Value)}"));
        return new Uri($"{endpoint.AbsoluteUri}?{query}", UriKind.Absolute);
    }

    private static void AddIfNotNull(IDictionary<string, string> parameters, string key, int? value)
    {
        if (value.HasValue)
        {
            parameters[key] = value.Value.ToString(CultureInfo.InvariantCulture);
        }
    }

    private static void AddIfNotBlank(IDictionary<string, string> parameters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value;
        }
    }
}
