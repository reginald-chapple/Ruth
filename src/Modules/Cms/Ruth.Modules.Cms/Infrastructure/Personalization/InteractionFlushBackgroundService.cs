using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ruth.Modules.Cms.Infrastructure.Personalization;

internal sealed partial class InteractionFlushBackgroundService(
    IInteractionBuffer buffer,
    IConfiguration configuration,
    ILogger<InteractionFlushBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Cms:InteractionFlushWorkerEnabled", defaultValue: true))
        {
            return;
        }

        var intervalMs = Math.Clamp(
            configuration.GetValue("Cms:InteractionFlushIntervalMs", 2000),
            250,
            60000
        );

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SafeFlushAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Graceful shutdown drain
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await SafeFlushAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async Task SafeFlushAsync(CancellationToken ct)
    {
        try
        {
            if (buffer.GetStatus().PendingApproximateCount > 0)
            {
                _ = await buffer.FlushAsync(ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogFlushFailed(logger, ex);
        }
    }

    [LoggerMessage(
        EventId = 6302,
        Level = LogLevel.Warning,
        Message = "Buffered CMS interaction flush encountered an error; items will retry."
    )]
    private static partial void LogFlushFailed(ILogger logger, Exception exception);
}
