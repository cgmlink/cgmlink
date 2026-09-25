using System.Net.Http;

namespace CgmLink.Nutrition.Tests;

internal sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    : HttpMessageHandler
{
    public int RequestCount { get; private set; }

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastContent { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestCount++;
        LastRequest = request;
        LastContent = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);
        return responseFactory(request);
    }
}
