using System;
using System.Collections.Generic;
using CgmLink.Nutrition.FatSecretClient.Exceptions;
using CgmLink.Nutrition.FatSecretClient.Json;
using Microsoft.Extensions.Options;

namespace CgmLink.Nutrition.FatSecretClient;

public interface IFatSecretAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    void Invalidate();

    void InvalidateIfCurrent(string accessToken) => Invalidate();
}

internal sealed class FatSecretAccessTokenProvider : IFatSecretAccessTokenProvider
{
    internal const string HttpClientName = "FatSecretOAuth";

    private const int RefreshMarginSeconds = 30;
    private const int DefaultTokenLifetimeSeconds = 86400;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FatSecretOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly object _stateLock = new();

    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public FatSecretAccessTokenProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<FatSecretOptions> options,
        TimeProvider? timeProvider = null)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var cachedToken = GetUsableToken();
        if (cachedToken is not null)
        {
            return cachedToken;
        }

        await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cachedToken = GetUsableToken();
            if (cachedToken is not null)
            {
                return cachedToken;
            }

            var token = await RequestTokenAsync(cancellationToken).ConfigureAwait(false);
            var accessToken = token.AccessToken
                ?? throw new FatSecretApiException("FatSecret OAuth2 token response did not contain an access token.");
            lock (_stateLock)
            {
                _accessToken = accessToken;
                _expiresAt = _timeProvider.GetUtcNow().AddSeconds(
                    Math.Max(1, token.ExpiresIn ?? DefaultTokenLifetimeSeconds));
            }

            return accessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public void Invalidate()
    {
        lock (_stateLock)
        {
            ClearCachedToken();
        }
    }

    public void InvalidateIfCurrent(string accessToken)
    {
        lock (_stateLock)
        {
            if (string.Equals(_accessToken, accessToken, StringComparison.Ordinal))
            {
                ClearCachedToken();
            }
        }
    }

    private string? GetUsableToken()
    {
        lock (_stateLock)
        {
            return _accessToken is not null
                && _timeProvider.GetUtcNow() < _expiresAt.AddSeconds(-RefreshMarginSeconds)
                ? _accessToken
                : null;
        }
    }

    private async Task<TokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        var options = _options.Value;
        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException("FatSecret OAuth2 credentials are not configured.");
        }

        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var request = new HttpRequestMessage(HttpMethod.Post, options.TokenUrl);
        var credentials = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = options.Scope,
        });

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw FatSecretApiException.FromResponse(content, (int)response.StatusCode);
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new FatSecretApiException("FatSecret OAuth2 token response did not contain an access token.");
        }

        var token = FatSecretJsonSerializer.Deserialize<TokenResponse>(
            content,
            (int)response.StatusCode,
            "FatSecret OAuth2 token response was not valid JSON.");

        if (string.IsNullOrWhiteSpace(token?.AccessToken))
        {
            throw new FatSecretApiException("FatSecret OAuth2 token response did not contain an access token.");
        }

        return token;
    }

    private void ClearCachedToken()
    {
        _accessToken = null;
        _expiresAt = DateTimeOffset.MinValue;
    }

    private sealed class TokenResponse
    {
        public string? AccessToken { get; set; }

        public int? ExpiresIn { get; set; }
    }
}
