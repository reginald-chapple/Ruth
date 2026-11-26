using System.Net;
using System.Text;

namespace Ruth.Modules.Communication.Tests.Transports;

internal sealed class RecordingHttpMessageHandler(
    HttpStatusCode responseStatus,
    string responseBody,
    KeyValuePair<string, string>? responseHeader = null
) : HttpMessageHandler
{
    public Uri? LastRequestUri { get; private set; }
    public string? LastAuthScheme { get; private set; }
    public string? LastAuthParameter { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        LastRequestUri = request.RequestUri;
        LastAuthScheme = request.Headers.Authorization?.Scheme;
        LastAuthParameter = request.Headers.Authorization?.Parameter;
        LastRequestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        var response = new HttpResponseMessage(responseStatus)
        {
            Content = new StringContent(responseBody, Encoding.UTF8, "application/json"),
        };
        if (responseHeader.HasValue)
        {
            _ = response.Headers.TryAddWithoutValidation(
                responseHeader.Value.Key,
                responseHeader.Value.Value
            );
        }

        return response;
    }
}
