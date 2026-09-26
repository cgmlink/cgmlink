using CgmLink.Nutrition.FatSecretClient.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CgmLink.Nutrition.FatSecretClient;

internal sealed class FatSecretAuthenticator : IFatSecretAuthenticator, IDisposable
{
    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(1);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly FatSecretOptions _options;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAt;

    public FatSecretAuthenticator(IHttpClientFactory httpClientFactory, IOptions<FatSecretOptions> options)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TokenIsValid())
        {
            return _accessToken!;
        }

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TokenIsValid())
            {
                return _accessToken!;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl);
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var formValues = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials")
            };
            if (!string.IsNullOrWhiteSpace(_options.Scope))
            {
                formValues.Add(new KeyValuePair<string, string>("scope", _options.Scope));
            }

            request.Content = new FormUrlEncodedContent(formValues);
            using var httpClient = _httpClientFactory.CreateClient("FatSecretAuth");
            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var token = await JsonSerializer.DeserializeAsync<TokenResponse>(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token?.AccessToken))
            {
                throw new InvalidOperationException("FatSecret returned an empty OAuth access token.");
            }

            _accessToken = token.AccessToken;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
            return _accessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task InvalidateAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (string.Equals(_accessToken, accessToken, StringComparison.Ordinal))
            {
                _accessToken = null;
                _expiresAt = default;
            }
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public void Dispose() => _tokenLock.Dispose();

    private bool TokenIsValid() =>
        !string.IsNullOrWhiteSpace(_accessToken) && DateTimeOffset.UtcNow < _expiresAt - RefreshBuffer;

}
