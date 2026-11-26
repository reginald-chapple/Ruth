namespace Ruth.Modules.CheckIn.Contracts.Sessions;

/// <summary>Classifies the type of search input submitted at a check-in kiosk or mobile client.</summary>
public enum KioskSearchInputKind
{
    /// <summary>Automatically infer barcode/phone/name based on input characters and configuration search mode.</summary>
    Auto = 0,

    /// <summary>Phone number digits (minimum 4 digits).</summary>
    Phone = 1,

    /// <summary>Participant or household name search.</summary>
    Name = 2,

    /// <summary>Scanned member card, barcode, QR token, or alternate search key.</summary>
    BarcodeOrCard = 3,
}
