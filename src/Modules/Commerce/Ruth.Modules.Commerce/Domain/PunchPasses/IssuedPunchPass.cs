using System.Security.Cryptography;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Commerce.Domain.PunchPasses;

/// <summary>Issued multi-visit or credit pack pass maintaining an append-only ledger and syncing balances to Smart Wallet Passes.</summary>
public sealed class IssuedPunchPass
    : AggregateRoot<IssuedPunchPassId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    public const string EntityTypeKeyName = "Ruth.Commerce.IssuedPunchPass";

    private readonly List<PunchPassLedgerEntry> _ledgerEntries = [];

    private IssuedPunchPass() { }

    private IssuedPunchPass(IssuedPunchPassId id)
        : base(id) { }

    public string PassCode { get; private set; } = string.Empty;
    public PunchPassTypeId PunchPassTypeId { get; private set; }
    public Guid OwnerPersonId { get; private set; }
    public CommerceOrderId? SourceOrderId { get; private set; }
    public int InitialCredits { get; private set; }
    public int RemainingCredits { get; private set; }
    public IssuedPunchPassStatus Status { get; private set; } = IssuedPunchPassStatus.Active;
    public Guid? AllowedBranchId { get; private set; }
    public bool AllowHouseholdSharing { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    public IReadOnlyCollection<PunchPassLedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? ModifiedOnUtc { get; private set; }
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    public string EntityTypeKey => EntityTypeKeyName;
    public Guid EntityId => Id.Value;
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    public static Result<IssuedPunchPass> Issue(
        PunchPassType passType,
        Guid ownerPersonId,
        int packMultiplier,
        CommerceOrderId? sourceOrderId,
        string? referenceNotes,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(passType);

        if (!passType.IsActive || ownerPersonId == Guid.Empty || packMultiplier <= 0)
        {
            return CommerceErrors.PunchPassRedemptionDenied;
        }

        var totalCredits = passType.CreditUnits * packMultiplier;
        var utcNow = nowUtc.ToUniversalTime();
        var pass = new IssuedPunchPass(IssuedPunchPassId.Create())
        {
            PassCode = GeneratePassCode(),
            PunchPassTypeId = passType.Id,
            OwnerPersonId = ownerPersonId,
            SourceOrderId = sourceOrderId,
            InitialCredits = totalCredits,
            RemainingCredits = totalCredits,
            Status = IssuedPunchPassStatus.Active,
            AllowedBranchId = passType.AllowedBranchId,
            AllowHouseholdSharing = passType.AllowHouseholdSharing,
            IssuedAtUtc = utcNow,
            ExpiresAtUtc = passType.ValidityDays.HasValue
                ? utcNow.AddDays(passType.ValidityDays.Value)
                : null,
        };

        pass._ledgerEntries.Add(
            new PunchPassLedgerEntry(
                PunchPassLedgerEntryId.Create(),
                pass.Id,
                PunchPassLedgerEntryKind.InitialGrant,
                totalCredits,
                totalCredits,
                ownerPersonId,
                referenceNotes ?? $"Issued {passType.Name}",
                utcNow
            )
        );

        return pass;
    }

    public bool IsUsableAt(DateTimeOffset nowUtc)
    {
        return Status == IssuedPunchPassStatus.Active
            && RemainingCredits > 0
            && (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > nowUtc);
    }

    public Result Redeem(
        int creditsToRedeem,
        Guid beneficiaryPersonId,
        bool beneficiaryIsHouseholdMember,
        Guid? branchId,
        string? referenceNotes,
        DateTimeOffset nowUtc
    )
    {
        if (ExpiresAtUtc.HasValue && ExpiresAtUtc.Value <= nowUtc)
        {
            Status = IssuedPunchPassStatus.Expired;
            return CommerceErrors.PunchPassRedemptionDenied;
        }

        if (
            !CanRedeemBalance(creditsToRedeem)
            || !CanRedeemInBranch(branchId)
            || !CanRedeemForBeneficiary(beneficiaryPersonId, beneficiaryIsHouseholdMember)
        )
        {
            return CommerceErrors.PunchPassRedemptionDenied;
        }

        RemainingCredits -= creditsToRedeem;
        if (RemainingCredits == 0)
        {
            Status = IssuedPunchPassStatus.Depleted;
        }

        _ledgerEntries.Add(
            new PunchPassLedgerEntry(
                PunchPassLedgerEntryId.Create(),
                Id,
                PunchPassLedgerEntryKind.Redemption,
                -creditsToRedeem,
                RemainingCredits,
                beneficiaryPersonId,
                referenceNotes,
                nowUtc
            )
        );
        return Result.Success();
    }

    public Result AdjustBalance(int deltaCredits, string reason, DateTimeOffset nowUtc)
    {
        if (
            deltaCredits == 0
            || string.IsNullOrWhiteSpace(reason)
            || Status == IssuedPunchPassStatus.Revoked
            || RemainingCredits + deltaCredits < 0
        )
        {
            return CommerceErrors.PunchPassRedemptionDenied;
        }

        RemainingCredits += deltaCredits;
        Status =
            RemainingCredits == 0 ? IssuedPunchPassStatus.Depleted : IssuedPunchPassStatus.Active;

        _ledgerEntries.Add(
            new PunchPassLedgerEntry(
                PunchPassLedgerEntryId.Create(),
                Id,
                PunchPassLedgerEntryKind.ManualAdjustment,
                deltaCredits,
                RemainingCredits,
                OwnerPersonId,
                reason,
                nowUtc
            )
        );
        return Result.Success();
    }

    public int RevokeForRefund(string reason, DateTimeOffset nowUtc)
    {
        var reversed = RemainingCredits;
        if (Status == IssuedPunchPassStatus.Revoked)
        {
            return 0;
        }

        RemainingCredits = 0;
        Status = IssuedPunchPassStatus.Revoked;
        if (reversed > 0)
        {
            _ledgerEntries.Add(
                new PunchPassLedgerEntry(
                    PunchPassLedgerEntryId.Create(),
                    Id,
                    PunchPassLedgerEntryKind.RefundReversal,
                    -reversed,
                    0,
                    OwnerPersonId,
                    reason,
                    nowUtc
                )
            );
        }

        return reversed;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        var changed = false;
        if (OwnerPersonId == retiredPersonId)
        {
            OwnerPersonId = survivingPersonId;
            changed = true;
        }

        foreach (var entry in _ledgerEntries)
        {
            changed |= entry.RepointBeneficiary(retiredPersonId, survivingPersonId);
        }

        return changed;
    }

    private bool CanRedeemBalance(int creditsToRedeem)
    {
        return Status == IssuedPunchPassStatus.Active
            && creditsToRedeem > 0
            && RemainingCredits >= creditsToRedeem;
    }

    private bool CanRedeemInBranch(Guid? branchId)
    {
        return !AllowedBranchId.HasValue
            || branchId is null
            || branchId.Value == Guid.Empty
            || AllowedBranchId.Value == branchId.Value;
    }

    private bool CanRedeemForBeneficiary(Guid beneficiaryPersonId, bool isHouseholdMember)
    {
        return beneficiaryPersonId == OwnerPersonId || (AllowHouseholdSharing && isHouseholdMember);
    }

    private static string GeneratePassCode()
    {
        Span<byte> bytes = stackalloc byte[5];
        RandomNumberGenerator.Fill(bytes);
        return $"PP-{Convert.ToHexString(bytes)}";
    }
}
