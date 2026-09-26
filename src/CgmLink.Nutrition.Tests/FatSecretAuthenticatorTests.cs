using CgmLink.Nutrition.FatSecretClient;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace CgmLink.Nutrition.Tests;

[TestFixture]
internal sealed class FatSecretAuthenticatorTests
{
    [Test]
    public async Task GetAccessTokenAsync_UsesClientCredentialsAndCachesToken()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"access_token":"token","token_type":"Bearer","expires_in":3600}""",
                Encoding.UTF8,
                "application/json")
        });
        using var httpClient = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(value => value.CreateClient("FatSecretAuth")).Returns(httpClient);
        using var sut = new FatSecretAuthenticator(factory.Object, Options.Create(new FatSecretOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            Scope = "premier barcode"
        }));

        var first = await sut.GetAccessTokenAsync();
        var second = await sut.GetAccessTokenAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("token"));
            Assert.That(second, Is.EqualTo("token"));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
            Assert.That(handler.LastRequest!.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(handler.LastRequest.Headers.Authorization, Is.EqualTo(
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(
                    Encoding.ASCII.GetBytes("client-id:client-secret")))));
            Assert.That(handler.LastContent, Does.Contain("grant_type=client_credentials"));
            Assert.That(handler.LastContent, Does.Contain("scope=premier+barcode"));
        });
    }

    [Test]
    public async Task InvalidateAccessTokenAsync_MatchingToken_FetchesReplacementToken()
    {
        var requestCount = 0;
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"token-{{++requestCount}}","expires_in":3600}""",
                Encoding.UTF8,
                "application/json")
        });
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(value => value.CreateClient("FatSecretAuth"))
            .Returns(() => new HttpClient(handler, disposeHandler: false));
        using var sut = new FatSecretAuthenticator(factory.Object, Options.Create(new FatSecretOptions
        {
            ClientId = "client-id",
            ClientSecret = "client-secret"
        }));

        var first = await sut.GetAccessTokenAsync();
        await sut.InvalidateAccessTokenAsync(first);
        var second = await sut.GetAccessTokenAsync();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo("token-1"));
            Assert.That(second, Is.EqualTo("token-2"));
            Assert.That(handler.RequestCount, Is.EqualTo(2));
        });
    }
}
