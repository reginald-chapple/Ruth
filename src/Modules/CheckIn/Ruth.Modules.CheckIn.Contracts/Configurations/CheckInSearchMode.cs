namespace Ruth.Modules.CheckIn.Contracts.Configurations;

/// <summary>Contract or identifier type CheckInSearchMode in the CheckIn module.</summary>
public enum CheckInSearchMode
{
    /// <summary>Allows phone number digits, name search, or barcode/pass scan.</summary>
    PhoneAndName = 0,

    /// <summary>Restricts manual entry to phone digits (plus barcode/pass scan).</summary>
    PhoneOnly = 1,

    /// <summary>Restricts manual entry to name search (plus barcode/pass scan).</summary>
    NameOnly = 2,

    /// <summary>Requires a scanned member card, QR code, or smart wallet pass.</summary>
    BarcodeAndPassOnly = 3,
}
