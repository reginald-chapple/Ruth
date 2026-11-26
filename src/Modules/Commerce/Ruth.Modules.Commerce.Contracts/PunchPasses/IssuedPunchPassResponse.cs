namespace Ruth.Modules.Commerce.Contracts.PunchPasses;

/// <summary>Read projection representing an issued punch pass and its credit balance ledger.</summary>
/// <param name="Id">Unique issued punch pass identifier.</param>
/// <param name="PassCode">Unique scan/lookup code (prefixed PP-).</param>
/// <param name="PunchPassTypeId">Parent PunchPassType identifier.</param>
/// <param name="PunchPassTypeName">Parent PunchPassType display name.</param>
/// <param name="OwnerPersonId">Holder CRM person identifier.</param>
/// <param name="SourceOrderId">Optional originating CommerceOrder identifier.</param>
/// <param name="InitialCredits">Initial credits granted.</param>
/// <param name="RemainingCredits">Current remaining credits.</param>
/// <param name="Status">Current pass status.</param>
/// <param name="AllowHouseholdSharing">Whether household members may redeem credits from this pass.</param>
/// <param name="IssuedAtUtc">UTC timestamp when issued.</param>
/// <param name="ExpiresAtUtc">Optional UTC expiration timestamp.</param>
/// <param name="LedgerEntries">Chronological balance ledger entries.</param>
public sealed record IssuedPunchPassResponse(
    Guid Id,
    string PassCode,
    Guid PunchPassTypeId,
    string PunchPassTypeName,
    Guid OwnerPersonId,
    Guid? SourceOrderId,
    int InitialCredits,
    int RemainingCredits,
    IssuedPunchPassStatus Status,
    bool AllowHouseholdSharing,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyList<PunchPassLedgerEntryResponse> LedgerEntries
);
