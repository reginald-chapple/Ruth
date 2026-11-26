using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Infrastructure.Printing;

/// <summary>Dispatches compiled Zebra ZPL II UTF-8 payloads over raw TCP sockets (port 9100) to local or VPN printers.</summary>
internal sealed class TcpRawSocketZplPrinterDispatcher(
    IOptions<ZplPrintingOptions> options,
    IClock clock
) : IZplPrinterDispatcher
{
    public async Task<Result<ZplPrintDispatchReceipt>> DispatchRawZplAsync(
        PrinterEndpointTarget target,
        string zplPayload,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(zplPayload))
        {
            return Result.Failure<ZplPrintDispatchReceipt>(CheckInErrors.PrinterDispatchFailed);
        }

        var cfg = options.Value;
        var bytes = Encoding.UTF8.GetBytes(zplPayload);
        var nowUtc = clock.UtcNow.ToUniversalTime();

        if (string.IsNullOrWhiteSpace(target.HostOrIpAddress))
        {
            return cfg.SimulateWhenEndpointUnconfigured
                ? Result.Success(
                    new ZplPrintDispatchReceipt(
                        target.PrinterName,
                        "simulated-local",
                        cfg.DefaultPort,
                        bytes.Length,
                        Simulated: true,
                        nowUtc
                    )
                )
                : Result.Failure<ZplPrintDispatchReceipt>(CheckInErrors.PrinterDispatchFailed);
        }

        var parsed = ParseEndpoint(target.HostOrIpAddress, cfg.DefaultPort);
        if (parsed.IsFailure)
        {
            return Result.Failure<ZplPrintDispatchReceipt>(parsed.Errors);
        }

        var (host, port) = parsed.Value;
        return await SendOverTcpAsync(
            target.PrinterName,
            host,
            port,
            bytes,
            cfg,
            nowUtc,
            cancellationToken
        );
    }

    private static Result<(string Host, int Port)> ParseEndpoint(
        string rawEndpoint,
        int defaultPort
    )
    {
        var trimmed = rawEndpoint.Trim();
        var colonIndex = trimmed.LastIndexOf(':');
        if (colonIndex > 0 && colonIndex < trimmed.Length - 1)
        {
            var hostPart = trimmed[..colonIndex].Trim();
            var portPart = trimmed[(colonIndex + 1)..].Trim();
            return
                int.TryParse(portPart, CultureInfo.InvariantCulture, out var parsedPort)
                && parsedPort is >= 1 and <= 65535
                && !string.IsNullOrWhiteSpace(hostPart)
                ? Result.Success((hostPart, parsedPort))
                : Result.Failure<(string, int)>(CheckInErrors.PrinterDispatchFailed);
        }

        return Result.Success((trimmed, Math.Clamp(defaultPort, 1, 65535)));
    }

    private static async Task<Result<ZplPrintDispatchReceipt>> SendOverTcpAsync(
        string printerName,
        string host,
        int port,
        byte[] bytes,
        ZplPrintingOptions cfg,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        try
        {
            using var tcpClient = new TcpClient();
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            connectCts.CancelAfter(Math.Clamp(cfg.ConnectTimeoutMs, 250, 30000));
            await tcpClient.ConnectAsync(host, port, connectCts.Token);

            await using var stream = tcpClient.GetStream();
            using var writeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            writeCts.CancelAfter(Math.Clamp(cfg.WriteTimeoutMs, 250, 30000));
            await stream.WriteAsync(bytes, writeCts.Token);
            await stream.FlushAsync(writeCts.Token);

            return Result.Success(
                new ZplPrintDispatchReceipt(
                    printerName,
                    host,
                    port,
                    bytes.Length,
                    Simulated: false,
                    nowUtc
                )
            );
        }
        catch (Exception ex)
            when (ex is SocketException or IOException or OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            return Result.Failure<ZplPrintDispatchReceipt>(CheckInErrors.PrinterDispatchFailed);
        }
    }
}
