using Ruth.Modules.CheckIn.Contracts.Sessions;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Sub-50ms Kiosk barcode/QR scan resolution outcome for an issued smart wallet pass.</summary>
/// <param name="Pass">Resolved active wallet pass summary.</param>
/// <param name="PersonId">Resolved holder CRM person identifier.</param>
/// <param name="ResolutionMilliseconds">Server-side scan lookup duration in milliseconds.</param>
/// <param name="KioskCandidates">Optional evaluated household and check-in room candidates when ConfigurationId and ScheduleId were supplied.</param>
public sealed record KioskWalletPassScanResponse(
    IssuedWalletPassResponse Pass,
    Guid PersonId,
    double ResolutionMilliseconds,
    KioskSearchResponse? KioskCandidates = null
);
