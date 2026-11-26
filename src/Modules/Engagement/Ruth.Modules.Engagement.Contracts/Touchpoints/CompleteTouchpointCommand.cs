using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.Touchpoints;

/// <summary>Command to complete a Touchpoint, recalculate relationship health, schedule next cadence, and sync CRM PeerNetwork.</summary>
/// <param name="TouchpointId">Target Touchpoint identifier.</param>
/// <param name="ChannelUsed">Channel actually used for outreach.</param>
/// <param name="OutcomeSentiment">Qualitative interaction sentiment.</param>
/// <param name="Summary">Optional interaction summary.</param>
/// <param name="UpdatedRelationshipStrength">Optional updated relationship strength.</param>
public sealed record CompleteTouchpointCommand(
    Guid TouchpointId,
    TouchpointChannel ChannelUsed,
    TouchpointOutcomeSentiment OutcomeSentiment,
    string? Summary = null,
    ContactRelationshipStrength? UpdatedRelationshipStrength = null
) : ICommand<TouchpointResponse>;
