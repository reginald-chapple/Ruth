namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Read projection of a single punch pass ledger entry.</summary>
/// <param name="Id">Unique ledger entry identifier.</param>
/// <param name="EntryKind">Type of ledger movement.</param>
/// <param name="DeltaCredits">Signed credit delta (&gt; 0 grant, &lt; 0 redemption/reversal).</param>
/// <param name="BalanceAfter">Resulting pass credit balance immediately after this entry.</param>
/// <param name="RedeemedForPersonId">Optional beneficiary person (owner or household member).</param>
/// <param name="ReferenceNotes">Optional audit memo or booking confirmation reference.</param>
/// <param name="OccurredAtUtc">UTC timestamp of the movement.</param>
public sealed record PunchPassLedgerEntryResponse(
    Guid Id,
    PunchPassLedgerEntryKind EntryKind,
    int DeltaCredits,
    int BalanceAfter,
    Guid? RedeemedForPersonId,
    string? ReferenceNotes,
    DateTimeOffset OccurredAtUtc
);
