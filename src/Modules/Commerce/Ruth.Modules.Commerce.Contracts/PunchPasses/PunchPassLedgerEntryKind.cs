namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Classification of an immutable credit/debit ledger entry on an IssuedPunchPass.</summary>
public enum PunchPassLedgerEntryKind
{
    /// <summary>Initial credits granted upon purchase or complimentary issuance.</summary>
    InitialGrant = 0,

    /// <summary>Credits redeemed for a facility visit, class, or service booking.</summary>
    Redemption = 1,

    /// <summary>Administrative credit or debit adjustment.</summary>
    ManualAdjustment = 2,

    /// <summary>Remaining credits reversed upon order refund or revocation.</summary>
    RefundReversal = 3,
}
