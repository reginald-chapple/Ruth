using System.Collections.Concurrent;
using System.Net;

namespace Ruth.Modules.VirtualRooms.Tests.Fixtures;

internal sealed class StubTwirpHttpClientFactory(
    Func<HttpRequestMessage, string, (HttpStatusCode Status, string JsonBody)> responder
) : IHttpClientFactory
{
    public ConcurrentQueue<(string Path, string? BearerJwt, string Body)> Requests { get; } = new();

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new TwirpHandler(this, responder), disposeHandler: true);
    }

    private sealed class TwirpHandler(
        StubTwirpHttpClientFactory parent,
        Func<HttpRequestMessage, string, (HttpStatusCode Status, string JsonBody)> responder
    ) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);
            parent.Requests.Enqueue(
                (
                    request.RequestUri?.AbsolutePath ?? string.Empty,
                    request.Headers.Authorization?.Parameter,
                    body
                )
            );
            var (status, json) = responder(request, body);
            return new HttpResponseMessage(status) { Content = new StringContent(json) };
        }
    }
}
