using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Updates global and per-topic bulk communication preferences for a recipient.</summary>
/// <param name="GlobalOptOut">Whether to opt out of all bulk messages on the channel.</param>
/// <param name="OptedOutTopics">Topic keys the recipient wishes to mute.</param>
/// <param name="PreferenceToken">Optional signed self-service preference token.</param>
/// <param name="DestinationAddress">Optional explicit destination address (for authenticated staff management).</param>
/// <param name="Channel">Target channel when updating by explicit destination.</param>
/// <param name="PersonId">Optional CRM person identifier.</param>
/// <param name="Reason">Optional audit note or reason.</param>
public sealed record UpdateRecipientPreferencesCommand(
    bool GlobalOptOut,
    IReadOnlyList<string>? OptedOutTopics = null,
    string? PreferenceToken = null,
    string? DestinationAddress = null,
    CommunicationChannel Channel = CommunicationChannel.Email,
    Guid? PersonId = null,
    string? Reason = null
) : ICommand<CommunicationPreferenceResponse>;
