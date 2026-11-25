namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Stable well-known entity type GUIDs owned by the CheckIn bounded context.</summary>
public static class CheckInSystemGuids
{
    /// <summary>EntityType registration GUID for CheckInConfiguration.</summary>
    public static readonly Guid CheckInConfiguration = new(
        0x0196f4a0,
        0x3201,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x03,
        0x01
    );

    /// <summary>EntityType registration GUID for CheckInArea.</summary>
    public static readonly Guid CheckInArea = new(
        0x0196f4a0,
        0x3201,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x03,
        0x02
    );

    /// <summary>EntityType registration GUID for CheckInAreaLocation.</summary>
    public static readonly Guid CheckInAreaLocation = new(
        0x0196f4a0,
        0x3201,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x03,
        0x03
    );
}
