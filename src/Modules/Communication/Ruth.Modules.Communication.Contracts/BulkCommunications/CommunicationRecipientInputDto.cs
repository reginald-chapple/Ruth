namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Input specification for adding an explicit recipient to a bulk or targeted communication.</summary>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="DestinationAddress">Optional explicit email address, E.164 phone number, or push token.</param>
/// <param name="DisplayName">Optional recipient display name.</param>
/// <param name="MergeFields">Optional per-recipient merge token key-value overrides.</param>
public sealed record CommunicationRecipientInputDto(
    Guid? PersonId = null,
    string? DestinationAddress = null,
    string? DisplayName = null,
    IReadOnlyDictionary<string, string?>? MergeFields = null
);
