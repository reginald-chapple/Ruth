namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Lifecycle state of a constituent or corporate financial pledge.</summary>
public enum FinancialPledgeStatus
{
    /// <summary>Commitment is currently active.</summary>
    Active = 0,

    /// <summary>Commitment has been fulfilled or closed at completion.</summary>
    Completed = 1,

    /// <summary>Commitment was cancelled prior to full fulfillment.</summary>
    Cancelled = 2,
}
