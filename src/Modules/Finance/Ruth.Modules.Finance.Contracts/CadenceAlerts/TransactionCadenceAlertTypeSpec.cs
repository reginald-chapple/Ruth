namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Specification payload for configuring a transaction cadence or spike alert rule.</summary>
/// <param name="Name">Rule display name.</param>
/// <param name="AlertKind">Target anomaly classification.</param>
/// <param name="MinHistoricalSamples">Minimum historical settled transactions required (at least 3).</param>
/// <param name="MultiplierThreshold">Multiple over mean interval or mean amount required (for example, 2.0 or 3.0).</param>
/// <param name="StandardDeviationSensitivity">Number of standard deviations above mean required (for example, 1.5).</param>
/// <param name="MinimumAmountThreshold">Minimum baseline or spike amount required to trigger.</param>
/// <param name="CooldownDays">Minimum days before emitting another alert of the same type for the same Person.</param>
/// <param name="AccountId">Optional filter restricting analysis to a single financial account.</param>
/// <param name="IsActive">Whether this alert rule is active during scans.</param>
public sealed record TransactionCadenceAlertTypeSpec(
    string Name,
    CadenceAlertKind AlertKind,
    int MinHistoricalSamples = 3,
    decimal MultiplierThreshold = 2.0m,
    decimal StandardDeviationSensitivity = 1.5m,
    decimal MinimumAmountThreshold = 50m,
    int CooldownDays = 30,
    Guid? AccountId = null,
    bool IsActive = true
);
