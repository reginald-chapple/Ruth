using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Executes RFC 8058 one-click unsubscribe using a signed recipient preference token.</summary>
/// <param name="PreferenceToken">Signed token embedded in the message unsubscribe URL.</param>
/// <param name="TopicKey">Optional topic key to unsubscribe from; when null or '*', opts out globally on the channel.</param>
public sealed record OneClickUnsubscribeCommand(string PreferenceToken, string? TopicKey = null)
    : ICommand<CommunicationPreferenceResponse>;
