using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Resolves a scanned WLT-* wallet pass barcode/QR token in sub-50ms and optionally evaluates Kiosk check-in options.</summary>
/// <param name="ScanToken">Scanned barcode or QR code payload.</param>
/// <param name="ConfigurationId">Optional check-in configuration identifier for immediate candidate evaluation.</param>
/// <param name="ScheduleId">Optional schedule identifier for immediate room balancing evaluation.</param>
/// <param name="ServiceDate">Optional service date override.</param>
public sealed record ResolveKioskWalletPassScanQuery(
    string ScanToken,
    Guid? ConfigurationId = null,
    Guid? ScheduleId = null,
    DateOnly? ServiceDate = null
) : IQuery<KioskWalletPassScanResponse>;
