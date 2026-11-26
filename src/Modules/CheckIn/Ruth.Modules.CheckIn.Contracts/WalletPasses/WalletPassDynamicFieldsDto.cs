namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Dynamic live-updatable fields rendered on an issued member, punch, shift, or event wallet pass.</summary>
/// <param name="HolderDisplayName">Cardholder display name printed on the pass front.</param>
/// <param name="MembershipTier">Current membership tier or credential level (for example Standard, Family, Gold).</param>
/// <param name="PunchOrCreditBalance">Remaining multi-visit punches or credit units when applicable.</param>
/// <param name="NextShiftOrBookingSummary">Upcoming volunteer shift, room booking, or appointment summary.</param>
/// <param name="EventTicketSummary">Event name, seat, or session admission summary.</param>
/// <param name="CustomFields">Optional additional auxiliary/back fields.</param>
public sealed record WalletPassDynamicFieldsDto(
    string? HolderDisplayName = null,
    string? MembershipTier = null,
    int? PunchOrCreditBalance = null,
    string? NextShiftOrBookingSummary = null,
    string? EventTicketSummary = null,
    IReadOnlyList<WalletPassCustomFieldDto>? CustomFields = null
);
