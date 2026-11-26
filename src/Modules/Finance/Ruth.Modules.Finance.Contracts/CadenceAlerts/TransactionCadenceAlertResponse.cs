namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Read projection for a detected constituent churn/lapse or unusual spike alert.</summary>
/// <param name="Id">Unique alert identifier.</param>
/// <param name="AlertTypeId">Originating alert rule identifier.</param>
/// <param name="AlertTypeName">Originating alert rule name.</param>
/// <param name="AlertKind">Anomaly classification.</param>
/// <param name="PersonId">Constituent or Business organization Person identifier.</param>
/// <param name="Status">Current review status.</param>
/// <param name="BaselineMeanAmount">Historical mean transaction amount.</param>
/// <param name="BaselineIntervalDays">Historical mean inter-transaction interval in days.</param>
/// <param name="ObservedValue">Observed days elapsed (for lapse) or observed transaction amount (for spike).</param>
/// <param name="Summary">Human-readable statistical explanation.</param>
/// <param name="ResolutionNotes">Optional staff follow-up notes.</param>
/// <param name="DetectedOnUtc">UTC timestamp when detected.</param>
public sealed record TransactionCadenceAlertResponse(
    Guid Id,
    Guid AlertTypeId,
    string AlertTypeName,
    CadenceAlertKind AlertKind,
    Guid PersonId,
    CadenceAlertStatus Status,
    decimal BaselineMeanAmount,
    decimal BaselineIntervalDays,
    decimal ObservedValue,
    string Summary,
    string? ResolutionNotes,
    DateTimeOffset DetectedOnUtc
);
