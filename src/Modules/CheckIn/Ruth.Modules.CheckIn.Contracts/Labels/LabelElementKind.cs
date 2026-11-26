namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Visual and ZPL primitive element kinds inside a <see cref="CheckInLabelLayoutDto" />.</summary>
public enum LabelElementKind
{
    /// <summary>Standard text or merge-token field (^FD).</summary>
    TextField = 0,

    /// <summary>High-contrast security pickup code block (supports ^FR reverse white-on-black).</summary>
    SecurityCodeBlock = 1,

    /// <summary>Medical, allergy, or compliance alert banner (supports conditional visibility and ^FR reverse).</summary>
    AlertBanner = 2,

    /// <summary>Code 128 1D barcode (^BCN).</summary>
    BarcodeCode128 = 3,

    /// <summary>2D QR code (^BQN).</summary>
    QrCode = 4,

    /// <summary>Graphic rectangle border or divider rule (^GB).</summary>
    BoxOrRule = 5,

    /// <summary>Membership tier or skill badge icon marker.</summary>
    BadgeIcon = 6,
}
