using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Retrieves subscription preferences using either a signed preference token or explicit destination.</summary>
/// <param name="PreferenceToken">Optional signed token from an email/SMS unsubscribe link.</param>
/// <param name="DestinationAddress">Optional explicit email address or E.164 phone (staff lookup).</param>
/// <param name="Channel">Channel when looking up by explicit destination.</param>
/// <param name="PersonId">Optional CRM person identifier.</param>
public sealed record GetRecipientPreferencesQuery(
    string? PreferenceToken = null,
    string? DestinationAddress = null,
    CommunicationChannel Channel = CommunicationChannel.Email,
    Guid? PersonId = null
) : IQuery<CommunicationPreferenceResponse>;
