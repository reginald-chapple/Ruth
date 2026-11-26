using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Renders and dispatches a transactional SystemCommunication across Email, SMS, and Push transports.</summary>
/// <param name="SystemCommunicationKey">Slug key of the target SystemCommunication (e.g., identity.password-reset).</param>
/// <param name="RecipientEmail">Optional recipient email address.</param>
/// <param name="RecipientName">Optional recipient display name.</param>
/// <param name="RecipientPhoneNumber">Optional recipient E.164 SMS phone number.</param>
/// <param name="RecipientPersonId">Optional CRM person identifier (used to resolve active PersonalDevice push tokens and person contact fallback).</param>
/// <param name="MergeFields">Optional flat or dotted merge token values (e.g., OrganizationName, ActionUrl, Amount).</param>
/// <param name="TriggerSource">Optional module or event trigger label for audit logging.</param>
public sealed record SendSystemCommunicationCommand(
    string SystemCommunicationKey,
    string? RecipientEmail = null,
    string? RecipientName = null,
    string? RecipientPhoneNumber = null,
    Guid? RecipientPersonId = null,
    IReadOnlyDictionary<string, string?>? MergeFields = null,
    string? TriggerSource = null
) : ICommand<SystemCommunicationSendResultResponse>;
