using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace Ruth.Modules.Workflow.Tests.Fixtures;

internal sealed class WorkflowStubHttpClientFactory(
    HttpStatusCode responseStatusCode = HttpStatusCode.Accepted
) : IHttpClientFactory
{
    public ConcurrentQueue<string> CapturedBodies { get; } = new();

    public HttpClient CreateClient(string name)
    {
        return new HttpClient(new StubHandler(this, responseStatusCode), disposeHandler: true);
    }

    private sealed class StubHandler(
        WorkflowStubHttpClientFactory parent,
        HttpStatusCode statusCode
    ) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (request.Content is not null)
            {
                parent.CapturedBodies.Enqueue(
                    await request.Content.ReadAsStringAsync(cancellationToken)
                );
            }

            return new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(new { accepted = true }),
            };
        }
    }
}
