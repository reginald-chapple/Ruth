using Ruth.Modules.Crm.Contracts.Relationships;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class RelationshipHealthCrmCollaborator(ICommandDispatcher commandDispatcher)
{
    public async Task SyncPeerNetworkEdgeAsync(
        ConstituentContact contact,
        DateOnly interactionDate,
        CancellationToken cancellationToken
    )
    {
        var crmStrength = contact.RelationshipStrength switch
        {
            ContactRelationshipStrength.Strong => RelationshipStrength.Deep,
            ContactRelationshipStrength.Established => RelationshipStrength.Close,
            ContactRelationshipStrength.Developing => RelationshipStrength.Casual,
            ContactRelationshipStrength.New => RelationshipStrength.Casual,
            ContactRelationshipStrength.AtRisk => RelationshipStrength.Casual,
            _ => RelationshipStrength.Casual,
        };

        var startDate =
            contact.CreatedOnUtc == default
                ? interactionDate
                : DateOnly.FromDateTime(contact.CreatedOnUtc.UtcDateTime);

        _ = await commandDispatcher.SendAsync<
            UpsertPeerNetworkEdgeCommand,
            PeerNetworkEdgeResponse
        >(
            new UpsertPeerNetworkEdgeCommand(
                contact.OwnerPersonId,
                contact.ConstituentPersonId,
                PeerNetworkRelationshipType.KnownRelationship,
                crmStrength,
                $"Portfolio {contact.PortfolioRole}",
                startDate <= interactionDate ? startDate : interactionDate,
                RelatedEntityId: contact.Id.Value
            ),
            cancellationToken
        );
    }
}
