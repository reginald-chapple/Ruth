using System.Diagnostics;

namespace Ruth.E2E.Tests.Fixtures;

public sealed class RunningHostProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task<string> _stdoutTask;
    private readonly Task<string> _stderrTask;
    private readonly HttpClient _httpClient;

    internal RunningHostProcess(
        Process process,
        Uri baseAddress,
        Task<string> stdoutTask,
        Task<string> stderrTask
    )
    {
        _process = process;
        BaseAddress = baseAddress;
        _stdoutTask = stdoutTask;
        _stderrTask = stderrTask;
        _httpClient = new HttpClient { BaseAddress = baseAddress };
    }

    public Uri BaseAddress { get; }

    public bool HasExited => _process.HasExited;

    public int? ExitCode => _process.HasExited ? _process.ExitCode : null;

    public HttpClient CreateNoRedirectClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        return new HttpClient(handler, disposeHandler: true) { BaseAddress = BaseAddress };
    }

    public Task<HttpResponseMessage> GetAsync(string relativePath, CancellationToken ct = default)
    {
        return _httpClient.GetAsync(new Uri(relativePath, UriKind.Relative), ct);
    }

    public async Task<ProcessRunResult> StopAndCollectAsync(CancellationToken ct = default)
    {
        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync(ct);
        }

        var stdout = await _stdoutTask;
        var stderr = await _stderrTask;
        return new ProcessRunResult(_process.ExitCode, stdout, stderr, TimedOut: false);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _process.WaitForExitAsync(cts.Token);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited or was never started.
        }
        catch (OperationCanceledException)
        {
            // Bounded shutdown wait elapsed.
        }
        finally
        {
            _httpClient.Dispose();
            _process.Dispose();
        }
    }
}
