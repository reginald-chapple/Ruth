using System.Globalization;
using System.Net;
using System.Text;

namespace Ruth.E2E.Tests.Tier2;

internal static class Tier2HttpHelpers
{
    public static Task<HttpResponseMessage> GetRelativeAsync(HttpClient client, string path)
    {
        return client.GetAsync(new Uri(path, UriKind.Relative));
    }

    public static Task<HttpResponseMessage> GetRelativeAsync(
        HttpClient client,
        string path,
        CancellationToken ct
    )
    {
        return client.GetAsync(new Uri(path, UriKind.Relative), ct);
    }

    public static async Task<HttpResponseMessage> PostEmptyAsync(HttpClient client, string path)
    {
        using var empty = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        return await client.PostAsync(new Uri(path, UriKind.Relative), empty);
    }

    public static async Task AssertHealthLiveOkAsync(HttpClient client)
    {
        using var resp = await GetRelativeAsync(client, "/health/live");
        Xunit.Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    public static string BuildDeadPostgresConn(int port, int timeoutSeconds = 2)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"Host=127.0.0.1;Port={port};Database=ruth;Username=ruth;"
                + $"Password=ruth_secret;Timeout={timeoutSeconds}"
        );
    }

    public static string BuildDeadRedisConn(int port, int connectTimeoutMs = 1000)
    {
        return string.Create(
            CultureInfo.InvariantCulture,
            $"127.0.0.1:{port},connectTimeout={connectTimeoutMs},abortConnect=false"
        );
    }

    public static string BuildLocalHttpUrl(int port)
    {
        return string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}");
    }
}
