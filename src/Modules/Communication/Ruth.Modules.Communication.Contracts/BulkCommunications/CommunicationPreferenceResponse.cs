using Ruth.Modules.Communication.Contracts.Transports;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Projection of a recipient's communication channel and topic opt-out preferences.</summary>
/// <param name="DestinationAddress">Normalized destination address (email or E.164 phone).</param>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="Channel">Channel scoped to these preferences.</param>
/// <param name="GlobalOptOut">Whether the recipient opted out of all bulk messages on this channel.</param>
/// <param name="OptedOutTopics">Specific topic keys opted out on this channel.</param>
/// <param name="AvailableTopics">Well-known organization topic keys presented in the Preference Center.</param>
/// <param name="PreferenceToken">Signed token permitting self-service updates in the Preference Center.</param>
/// <param name="UpdatedAtUtc">Last preference change timestamp (UTC).</param>
public sealed record CommunicationPreferenceResponse(
    string DestinationAddress,
    Guid? PersonId,
    CommunicationChannel Channel,
    bool GlobalOptOut,
    IReadOnlyList<string> OptedOutTopics,
    IReadOnlyList<string> AvailableTopics,
    string PreferenceToken,
    DateTimeOffset? UpdatedAtUtc
);
