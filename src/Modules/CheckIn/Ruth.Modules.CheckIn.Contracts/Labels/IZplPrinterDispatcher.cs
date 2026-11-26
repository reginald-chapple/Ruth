using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Strategy abstraction dispatching compiled Zebra ZPL II payloads over raw TCP sockets (port 9100) to local or VPN printers.</summary>
public interface IZplPrinterDispatcher
{
    /// <summary>Transmits <paramref name="zplPayload" /> to <paramref name="target" /> asynchronously.</summary>
    Task<Result<ZplPrintDispatchReceipt>> DispatchRawZplAsync(
        PrinterEndpointTarget target,
        string zplPayload,
        CancellationToken cancellationToken = default
    );
}
