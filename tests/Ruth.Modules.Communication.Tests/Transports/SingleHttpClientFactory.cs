namespace Ruth.Modules.Communication.Tests.Transports;

internal sealed class SingleHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient(handler, disposeHandler: false);
    }
}
