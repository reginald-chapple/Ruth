namespace Ruth.Modules.Communication.Contracts.Flows;

/// <summary>Recipient input entry when launching or enrolling into a CommunicationFlowInstance.</summary>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="EmailAddress">Optional recipient email address.</param>
/// <param name="PhoneNumber">Optional recipient E.164 SMS phone number.</param>
/// <param name="DisplayName">Optional display name.</param>
/// <param name="MergeFields">Optional per-recipient merge values.</param>
public sealed record FlowRecipientEnrollmentInputDto(
    Guid? PersonId = null,
    string? EmailAddress = null,
    string? PhoneNumber = null,
    string? DisplayName = null,
    IReadOnlyDictionary<string, string?>? MergeFields = null
);
