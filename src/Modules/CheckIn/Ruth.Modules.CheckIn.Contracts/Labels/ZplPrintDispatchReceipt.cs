namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Receipt returned after dispatching a raw Zebra ZPL II payload to a network printer.</summary>
/// <param name="PrinterName">Target printer name.</param>
/// <param name="ResolvedHost">Resolved host/IP connected to.</param>
/// <param name="ResolvedPort">Resolved TCP port (typically 9100).</param>
/// <param name="BytesSent">Total UTF-8 ZPL bytes transmitted.</param>
/// <param name="Simulated">Whether delivery was simulated because no physical printer IP was configured.</param>
/// <param name="DispatchedOnUtc">UTC dispatch timestamp.</param>
public sealed record ZplPrintDispatchReceipt(
    string PrinterName,
    string ResolvedHost,
    int ResolvedPort,
    int BytesSent,
    bool Simulated,
    DateTimeOffset DispatchedOnUtc
);
