namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Read projection for a transaction cadence or spike alert rule definition.</summary>
/// <param name="Id">Alert type identifier.</param>
/// <param name="Name">Rule display name.</param>
/// <param name="AlertKind">Target anomaly classification.</param>
/// <param name="MinHistoricalSamples">Minimum required historical transactions.</param>
/// <param name="MultiplierThreshold">Ratio multiplier over historical mean.</param>
/// <param name="StandardDeviationSensitivity">Standard deviations above mean threshold.</param>
/// <param name="MinimumAmountThreshold">Minimum monetary threshold.</param>
/// <param name="CooldownDays">Deduplication cooldown window in days.</param>
/// <param name="AccountId">Optional scoped financial account identifier.</param>
/// <param name="IsActive">Whether active.</param>
public sealed record TransactionCadenceAlertTypeResponse(
    Guid Id,
    string Name,
    CadenceAlertKind AlertKind,
    int MinHistoricalSamples,
    decimal MultiplierThreshold,
    decimal StandardDeviationSensitivity,
    decimal MinimumAmountThreshold,
    int CooldownDays,
    Guid? AccountId,
    bool IsActive
);
