namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Type of alternate lookup key associated with a person.</summary>
public enum PersonSearchKeyType
{
    /// <summary>System-generated unique alternate identifier (xxxxxxx-xxxxxxx).</summary>
    AlternateId = 0,

    /// <summary>Alternate or archived email address.</summary>
    Email = 1,

    /// <summary>Check-in or membership barcode.</summary>
    Barcode = 2,

    /// <summary>Imported legacy system identifier.</summary>
    LegacyId = 3,

    /// <summary>External system identifier.</summary>
    ExternalId = 4,
}
