using Ruth.Modules.Finance.Contracts.CadenceAlerts;

namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>Statistical anomaly finding emitted by <see cref="TransactionCadenceAnalyzer" />.</summary>
public readonly record struct CadenceDetectionFinding(
    CadenceAlertKind Kind,
    decimal BaselineMeanAmount,
    decimal BaselineIntervalDays,
    decimal ObservedValue,
    string Summary
);
