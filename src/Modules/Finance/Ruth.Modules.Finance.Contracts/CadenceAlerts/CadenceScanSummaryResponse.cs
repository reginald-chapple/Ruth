namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Summary returned after running a statistical cadence and spike detection pass.</summary>
/// <param name="EvaluatedConstituentCount">Number of constituents analyzed.</param>
/// <param name="AlertsCreatedCount">Number of new cadence/spike alerts created.</param>
public sealed record CadenceScanSummaryResponse(
    int EvaluatedConstituentCount,
    int AlertsCreatedCount
);
