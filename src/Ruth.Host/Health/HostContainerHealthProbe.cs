using System.Globalization;

namespace Ruth.Host.Health;

/// <summary>
/// Shell-less CLI liveness probe invoked via <c lang="text">dotnet Ruth.Host.dll --healthcheck</c>
/// inside chiseled distroless containers that omit <c lang="text">/bin/sh</c> and <c lang="text">curl</c>.
/// </summary>
public static class HostContainerHealthProbe
{
    /// <summary>Command-line switch triggering the self-contained HTTP liveness check.</summary>
    public const string CliSwitch = "--healthcheck";

    /// <summary>Returns true when <paramref name="args" /> requests a container liveness probe.</summary>
    public static bool IsHealthCheckInvocation(string[]? args)
    {
        return args is { Length: > 0 }
            && string.Equals(args[0], CliSwitch, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Probes local <c lang="text">/health/live</c> and returns POSIX exit code 0 when healthy or 1 otherwise.</summary>
    public static async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var port = ResolveListeningPort(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
        var targetUri = new Uri(
            string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}/health/live"),
            UriKind.Absolute
        );

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(3));

        using var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseProxy = false };
        using var client = new HttpClient(handler);
        try
        {
            using var response = await client.GetAsync(
                targetUri,
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token
            );
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception)
        {
            return 1;
        }
    }

    /// <summary>Extracts the primary TCP port from <c lang="text">ASPNETCORE_URLS</c>, defaulting to 8080.</summary>
    public static int ResolveListeningPort(string? aspNetCoreUrls)
    {
        if (string.IsNullOrWhiteSpace(aspNetCoreUrls))
        {
            return 8080;
        }

        var firstBinding = aspNetCoreUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries)[0]
            .Trim();
        var normalized = firstBinding
            .Replace("://+", "://127.0.0.1", StringComparison.Ordinal)
            .Replace("://*", "://127.0.0.1", StringComparison.Ordinal);

        return Uri.TryCreate(normalized, UriKind.Absolute, out var parsed) && parsed.Port > 0
            ? parsed.Port
            : 8080;
    }
}
