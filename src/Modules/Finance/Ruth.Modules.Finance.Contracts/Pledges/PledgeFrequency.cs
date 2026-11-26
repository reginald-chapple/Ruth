namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Cadence at which a financial pledge or multi-period commitment is expected to be fulfilled.</summary>
public enum PledgeFrequency
{
    /// <summary>Single lump-sum commitment.</summary>
    OneTime = 0,

    /// <summary>Weekly installments (every 7 days).</summary>
    Weekly = 1,

    /// <summary>Bi-weekly installments (every 14 days).</summary>
    BiWeekly = 2,

    /// <summary>Semi-monthly installments (twice per month).</summary>
    SemiMonthly = 3,

    /// <summary>Monthly installments.</summary>
    Monthly = 4,

    /// <summary>Quarterly installments (every 3 months).</summary>
    Quarterly = 5,

    /// <summary>Annual installments.</summary>
    Annually = 6,
}
