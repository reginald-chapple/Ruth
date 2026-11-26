using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Issues a new smart wallet pass for a participant and optionally dispatches an email/SMS claim link.</summary>
/// <param name="TemplateId">Target wallet pass template identifier.</param>
/// <param name="PersonId">Holder CRM person identifier.</param>
/// <param name="DynamicFields">Initial cardholder, tier, punch/credit balance, shift, or event fields.</param>
/// <param name="ExpiresAtUtc">Optional explicit expiration timestamp in UTC.</param>
/// <param name="SendClaimNotification">When true, enqueues a transactional email/SMS pass claim notification.</param>
/// <param name="RecipientEmail">Optional email override for claim delivery.</param>
/// <param name="RecipientPhoneNumber">Optional E.164 SMS phone override for claim delivery.</param>
/// <param name="PublicBaseUrl">Optional public base URL used to format absolute claim and PassKit callback URLs.</param>
public sealed record IssueWalletPassCommand(
    Guid TemplateId,
    Guid PersonId,
    WalletPassDynamicFieldsDto DynamicFields,
    DateTimeOffset? ExpiresAtUtc = null,
    bool SendClaimNotification = false,
    string? RecipientEmail = null,
    string? RecipientPhoneNumber = null,
    string? PublicBaseUrl = null
) : ICommand<IssueWalletPassResultResponse>;
