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

    /// <summary>EntityType registration GUID for AttendanceCheckInSession.</summary>
    public static readonly Guid AttendanceCheckInSession = new(
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
        0x04
    );

    /// <summary>EntityType registration GUID for CheckInAttendanceEntry.</summary>
    public static readonly Guid CheckInAttendanceEntry = new(
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
        0x05
    );

    /// <summary>EntityType registration GUID for CheckInLabel.</summary>
    public static readonly Guid CheckInLabel = new(
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
        0x06
    );

    /// <summary>EntityType registration GUID for WalletPassTemplate.</summary>
    public static readonly Guid WalletPassTemplate = new(
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
        0x07
    );

    /// <summary>EntityType registration GUID for IssuedWalletPass.</summary>
    public static readonly Guid IssuedWalletPass = new(
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
        0x08
    );

    /// <summary>Seeded system WalletPassTemplate: Standard Member &amp; Access Pass.</summary>
    public static readonly Guid SeededMemberWalletPassTemplate = new(
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
        0x11
    );
}
