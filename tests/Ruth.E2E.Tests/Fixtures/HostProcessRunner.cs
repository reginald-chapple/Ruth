using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

namespace Ruth.E2E.Tests.Fixtures;

public static class HostProcessRunner
{
    private static readonly string[] _isolatedEnvPrefixes =
    [
        "ASPNETCORE_",
        "DOTNET_",
        "Authentication__",
        "ConnectionStrings__",
        "Storage__",
        "RustFs__",
        "RateLimiting__",
        "HealthChecks__",
        "Observability__",
        "OpenTelemetry__",
        "OTEL_",
        "OpenApi__",
        "Bootstrap__",
        "Logging__",
        "Outbox__",
        "Scheduler__",
    ];

    public static async Task<ProcessRunResult> RunHostAsync(
        IReadOnlyDictionary<string, string?> environmentVariables,
        TimeSpan timeout,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(environmentVariables);

        var (startInfo, _) = BuildStartInfo(environmentVariables);
        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start dotnet Ruth.Host.dll process.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        var timedOut = false;
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            timedOut = true;
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var exitCode = timedOut ? -1 : process.ExitCode;

        return new ProcessRunResult(exitCode, stdout, stderr, timedOut);
    }

    public static async Task<RunningHostProcess> StartHostAsync(
        IReadOnlyDictionary<string, string?> environmentVariables,
        TimeSpan startupTimeout,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(environmentVariables);

        var (startInfo, baseUri) = BuildStartInfo(environmentVariables);
        var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            process.Dispose();
            throw new InvalidOperationException("Failed to start dotnet Ruth.Host.dll process.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var stderrTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        var runningHost = new RunningHostProcess(process, baseUri, stdoutTask, stderrTask);

        try
        {
            await WaitUntilLiveAsync(runningHost, startupTimeout, ct);
            return runningHost;
        }
        catch
        {
            await runningHost.DisposeAsync();
            throw;
        }
    }

    private static async Task WaitUntilLiveAsync(
        RunningHostProcess runningHost,
        TimeSpan startupTimeout,
        CancellationToken ct
    )
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(startupTimeout);

        while (!timeoutCts.IsCancellationRequested)
        {
            if (runningHost.HasExited)
            {
                var result = await runningHost.StopAndCollectAsync(ct);
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Ruth.Host exited prematurely with code {0}. Stdout: {1} Stderr: {2}",
                        result.ExitCode,
                        result.StandardOutput,
                        result.StandardError
                    )
                );
            }

            if (await TryProbeLiveAsync(runningHost, timeoutCts.Token))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150), timeoutCts.Token);
        }

        throw new TimeoutException(
            string.Format(
                CultureInfo.InvariantCulture,
                "Ruth.Host did not become live at {0}/health/live within {1}.",
                runningHost.BaseAddress,
                startupTimeout
            )
        );
    }

    private static async Task<bool> TryProbeLiveAsync(
        RunningHostProcess runningHost,
        CancellationToken ct
    )
    {
        try
        {
            using var response = await runningHost.GetAsync("/health/live", ct);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return false;
        }
    }

    private static (ProcessStartInfo StartInfo, Uri BaseUri) BuildStartInfo(
        IReadOnlyDictionary<string, string?> environmentVariables
    )
    {
        var hostDllPath = ResolveHostAssemblyPath();
        var hostDirectory = Path.GetDirectoryName(hostDllPath)
            ?? throw new InvalidOperationException("Unable to determine Ruth.Host directory.");

        var port = GetAvailablePort();
        var baseUrl = string.Create(CultureInfo.InvariantCulture, $"http://127.0.0.1:{port}");

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = hostDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add(hostDllPath);

        var keysToRemove = startInfo
            .Environment.Keys.Where(key =>
                _isolatedEnvPrefixes.Any(prefix =>
                    key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                )
            )
            .ToList();

        foreach (var key in keysToRemove)
        {
            _ = startInfo.Environment.Remove(key);
        }

        startInfo.Environment["ASPNETCORE_URLS"] = baseUrl;

        foreach (var (key, value) in environmentVariables)
        {
            if (value is null)
            {
                _ = startInfo.Environment.Remove(key);
            }
            else
            {
                startInfo.Environment[key] = value;
            }
        }

        var effectiveUrl = startInfo.Environment["ASPNETCORE_URLS"] ?? baseUrl;
        return (startInfo, new Uri(effectiveUrl, UriKind.Absolute));
    }

    private static string ResolveHostAssemblyPath()
    {
        var baseCandidate = Path.Combine(AppContext.BaseDirectory, "Ruth.Host.dll");
        if (File.Exists(baseCandidate))
        {
            return baseCandidate;
        }

        var assemblyLocation = typeof(Program).Assembly.Location;
        return !string.IsNullOrWhiteSpace(assemblyLocation) && File.Exists(assemblyLocation)
            ? assemblyLocation
            : throw new FileNotFoundException(
                "Could not locate Ruth.Host.dll in test output directory.",
                baseCandidate
            );
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
