namespace Ruth.Modules.Commerce.Contracts.Catalog;

/// <summary>Classifies the fulfillment and entitlement behavior of a commerce catalog product.</summary>
public enum ProductKind
{
    /// <summary>Physical merchandise, books, or kits fulfilled via local Branch pickup or flat-rate shipping.</summary>
    PhysicalPickupOrFlatRate = 0,

    /// <summary>Digital downloadable asset stored in RustFS via Core BinaryFiles.</summary>
    DigitalDownload = 1,

    /// <summary>Multi-visit facility pass or coaching/service credit pack issuing an IssuedPunchPass.</summary>
    PunchPassPack = 2,
}
