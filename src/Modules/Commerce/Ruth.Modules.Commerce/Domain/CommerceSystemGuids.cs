namespace Ruth.Modules.Commerce.Domain;

/// <summary>Deterministic UUIDv7 system identifiers for Commerce module entity type registrations.</summary>
public static class CommerceSystemGuids
{
    /// <summary>Entity type GUID for PunchPassType.</summary>
    public static readonly Guid PunchPassType = new(
        0x0196c000,
        0x0901,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x09,
        0x01
    );

    /// <summary>Entity type GUID for Product.</summary>
    public static readonly Guid Product = new(
        0x0196c000,
        0x0902,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x09,
        0x02
    );

    /// <summary>Entity type GUID for MemberPricingRule.</summary>
    public static readonly Guid MemberPricingRule = new(
        0x0196c000,
        0x0903,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x09,
        0x03
    );

    /// <summary>Entity type GUID for CommerceOrder.</summary>
    public static readonly Guid CommerceOrder = new(
        0x0196c000,
        0x0904,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x09,
        0x04
    );

    /// <summary>Entity type GUID for IssuedPunchPass.</summary>
    public static readonly Guid IssuedPunchPass = new(
        0x0196c000,
        0x0905,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x09,
        0x05
    );

    /// <summary>Entity type GUID for BookableService.</summary>
    public static readonly Guid BookableService = new(
        0x0196c000,
        0x0a01,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x0a,
        0x01
    );

    /// <summary>Entity type GUID for RentableResource.</summary>
    public static readonly Guid RentableResource = new(
        0x0196c000,
        0x0a02,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x0a,
        0x02
    );

    /// <summary>Entity type GUID for AvailabilityRule.</summary>
    public static readonly Guid AvailabilityRule = new(
        0x0196c000,
        0x0a03,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x0a,
        0x03
    );

    /// <summary>Entity type GUID for ServiceOrResourceBooking.</summary>
    public static readonly Guid ServiceBooking = new(
        0x0196c000,
        0x0a04,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x0a,
        0x04
    );
}
