namespace Ruth.Modules.Finance.Domain.CadenceAlerts;

/// <summary>Chronological settled transaction sample point evaluated by <see cref="TransactionCadenceAnalyzer" />.</summary>
public readonly record struct HistoricalTransactionPoint(DateOnly Date, decimal NetAmount);
