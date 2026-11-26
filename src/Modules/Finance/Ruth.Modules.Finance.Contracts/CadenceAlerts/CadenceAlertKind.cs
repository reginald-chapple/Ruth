namespace Ruth.Modules.Finance.Contracts.CadenceAlerts;

/// <summary>Classification of statistical transaction cadence or billing health alerts.</summary>
public enum CadenceAlertKind
{
    /// <summary>Constituent exceeded expected inter-transaction cadence window (churn or lapse risk).</summary>
    LapseOrChurnRisk = 0,

    /// <summary>New settled transaction exceeded expected historical amount thresholds (unusual spike).</summary>
    UnusualAmountSpike = 1,

    /// <summary>Active recurring subscription or gift entered PastDue or failed billing repeatedly.</summary>
    SubscriptionPastDue = 2,
}
