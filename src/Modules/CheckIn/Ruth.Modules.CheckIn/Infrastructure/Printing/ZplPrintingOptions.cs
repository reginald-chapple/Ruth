namespace Ruth.Modules.CheckIn.Infrastructure.Printing;

/// <summary>Configuration options for server-side Zebra ZPL raw TCP socket printing.</summary>
public sealed class ZplPrintingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "CheckIn:ZplPrinting";

    /// <summary>Gets or sets the default Zebra raw TCP port (standard 9100).</summary>
    public int DefaultPort { get; set; } = 9100;

    /// <summary>Gets or sets the TCP connect timeout in milliseconds.</summary>
    public int ConnectTimeoutMs { get; set; } = 3000;

    /// <summary>Gets or sets the socket write timeout in milliseconds.</summary>
    public int WriteTimeoutMs { get; set; } = 5000;

    /// <summary>Gets or sets whether missing/empty printer IP addresses simulate dispatch cleanly in development.</summary>
    public bool SimulateWhenEndpointUnconfigured { get; set; } = true;
}
