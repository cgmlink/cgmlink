using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CgmLink.Nutrition.FatSecretClient.Exceptions;
using CgmLink.Nutrition.FatSecretClient.Models;
using Microsoft.Extensions.Options;

namespace CgmLink.Nutrition.FatSecretClient;

internal sealed class FatSecretClient : IFatSecretClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly HttpClient _httpClient;
    private readonly IFatSecretAccessTokenProvider _accessTokenProvider;
    private readonly IOptions<FatSecretOptions> _options;

    internal FatSecretClient(
        HttpClient httpClient,
        IOptions<FatSecretOptions> options)
        : this(
            httpClient,
            new FatSecretAccessTokenProvider(httpClient, options),
            options)
    {
    }

    public FatSecretClient(
        HttpClient httpClient,
        IFatSecretAccessTokenProvider accessTokenProvider,
        IOptions<FatSecretOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _accessTokenProvider = accessTokenProvider ?? throw new ArgumentNullException(nameof(accessTokenProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<FatSecretSearchResults> SearchAsync(
        string searchExpression,
        int? pageNumber = null,
        int? maxResults = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(searchExpression);

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
        var items = data?.Food.Select(item => new FatSecretFoodSearchResult
        {
            ProductId = item.FoodId ?? "",
            Name = item.FoodName ?? "",
        })
            .ToArray() ?? [];

        return new FatSecretSearchResults(
            items,
            data?.TotalResults,
            data?.PageNumber,
            data?.MaxResults);
    }

    public async Task<FatSecretFood?> GetFoodAsync(
        string foodId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(foodId);

        var parameters = new Dictionary<string, string>
        {
            ["method"] = "food.get",
            ["food_id"] = foodId,
            ["format"] = "json",
        };

        var response = await RequestAsync<FoodGetResponse>(parameters, cancellationToken).ConfigureAwait(false);
        var food = response?.Food;
        return food is null
            ? null
            : new FatSecretFood
            {
                ProductId = food.FoodId ?? "",
                Name = food.FoodName ?? "",
                Servings = food.Servings?.Serving.Select(serving => new FatSecretFoodServing
                {
                    ExternalId = serving.ServingId,
                    Description = serving.ServingDescription,
                    ServingAmount = serving.MetricServingAmount,
                    ServingUnit = serving.MetricServingUnit,
                    Calories = serving.Calories,
                    Carbs = serving.Carbohydrate,
                    Protein = serving.Protein,
                    Fat = serving.Fat,
                })
                    .ToArray() ?? [],
            };
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

        using var document = JsonDocument.Parse(content);
        if (document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty("error", out _))
        {
            var exception = FatSecretApiException.FromResponse(content, (int)response.StatusCode);
            if (allowUnauthorizedRetry && IsAuthenticationFailure(response.StatusCode, exception))
            {
                _accessTokenProvider.InvalidateIfCurrent(accessToken);
                return await RequestAsync<T>(parameters, cancellationToken, false).ConfigureAwait(false);
            }

            throw exception;
        }

        return document.RootElement.Deserialize<T>(JsonOptions);
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
