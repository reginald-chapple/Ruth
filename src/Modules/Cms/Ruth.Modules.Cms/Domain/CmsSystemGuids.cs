namespace Ruth.Modules.Cms.Domain;

/// <summary>Deterministic well-known system GUIDs owned by the Cms module.</summary>
public static class CmsSystemGuids
{
    /// <summary>EntityType GUID for CMS Site.</summary>
    public static readonly Guid Site = new(
        0x70000001,
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

    /// <summary>EntityType GUID for CMS Page.</summary>
    public static readonly Guid Page = new(
        0x70000001,
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

    /// <summary>EntityType GUID for CMS Block.</summary>
    public static readonly Guid Block = new(
        0x70000001,
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

    /// <summary>EntityType GUID for CMS ContentChannel.</summary>
    public static readonly Guid ContentChannel = new(
        0x70000001,
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

    /// <summary>EntityType GUID for CMS ContentChannelItem.</summary>
    public static readonly Guid ContentChannelItem = new(
        0x70000001,
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

    /// <summary>EntityType GUID for CMS PersonalizationSegment.</summary>
    public static readonly Guid PersonalizationSegment = new(
        0x70000001,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x06
    );

    /// <summary>EntityType GUID for CMS AdaptiveMessage.</summary>
    public static readonly Guid AdaptiveMessage = new(
        0x70000001,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x07
    );

    /// <summary>EntityType GUID for CMS MobileApplication.</summary>
    public static readonly Guid MobileApplication = new(
        0x70000001,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x08
    );

    /// <summary>EntityType GUID for CMS SignageApplication.</summary>
    public static readonly Guid SignageApplication = new(
        0x70000001,
        0x0000,
        0x7000,
        0x80,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x00,
        0x09
    );
}
