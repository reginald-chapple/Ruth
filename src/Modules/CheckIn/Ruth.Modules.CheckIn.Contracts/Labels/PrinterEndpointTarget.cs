namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Network endpoint target for server-side raw TCP Zebra ZPL dispatch.</summary>
/// <param name="PrinterName">Human-readable printer device name.</param>
/// <param name="HostOrIpAddress">IPv4/IPv6 address or hostname (optional ":port" suffix; defaults to TCP 9100).</param>
/// <param name="PrinterDeviceId">Optional Core Device identifier.</param>
public sealed record PrinterEndpointTarget(
    string PrinterName,
    string HostOrIpAddress,
    Guid? PrinterDeviceId = null
);
