using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain.PunchPasses;

/// <summary>Immutable balance movement recorded on an <see cref="IssuedPunchPass" />.</summary>
public sealed class PunchPassLedgerEntry : Entity<PunchPassLedgerEntryId>
{
    private PunchPassLedgerEntry() { }

    internal PunchPassLedgerEntry(
        PunchPassLedgerEntryId id,
        IssuedPunchPassId issuedPunchPassId,
        PunchPassLedgerEntryKind entryKind,
        int deltaCredits,
        int balanceAfter,
        Guid? redeemedForPersonId,
        string? referenceNotes,
        DateTimeOffset occurredAtUtc
    )
        : base(id)
    {
        IssuedPunchPassId = issuedPunchPassId;
        EntryKind = entryKind;
        DeltaCredits = deltaCredits;
        BalanceAfter = balanceAfter;
        RedeemedForPersonId = redeemedForPersonId;
        ReferenceNotes = string.IsNullOrWhiteSpace(referenceNotes) ? null : referenceNotes.Trim();
        OccurredAtUtc = occurredAtUtc.ToUniversalTime();
    }

    public IssuedPunchPassId IssuedPunchPassId { get; private set; }
    public PunchPassLedgerEntryKind EntryKind { get; private set; }
    public int DeltaCredits { get; private set; }
    public int BalanceAfter { get; private set; }
    public Guid? RedeemedForPersonId { get; private set; }
    public string? ReferenceNotes { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }

    internal bool RepointBeneficiary(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (RedeemedForPersonId != retiredPersonId)
        {
            return false;
        }

        RedeemedForPersonId = survivingPersonId;
        return true;
    }
}
