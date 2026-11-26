namespace Ruth.Modules.Reporting.Domain;

/// <summary>Deterministic well-known system GUIDs owned by the Reporting module.</summary>
public static class ReportingSystemGuids
{
    public static readonly Guid DataView = new(
        0x70000002,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x01
    );

    public static readonly Guid Report = new(
        0x70000002,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x02
    );

    public static readonly Guid MergeTemplate = new(
        0x70000002,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x03
    );

    public static readonly Guid MetricCategory = new(
        0x70000002,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x04
    );

    public static readonly Guid Metric = new(
        0x70000002,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x05
    );
}
