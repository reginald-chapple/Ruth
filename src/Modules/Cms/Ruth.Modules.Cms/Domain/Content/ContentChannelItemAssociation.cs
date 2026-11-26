using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain.Content;

/// <summary>Directional association linking a parent <see cref="ContentChannelItem" /> to a child item.</summary>
public sealed class ContentChannelItemAssociation : Entity<ContentChannelItemAssociationId>
{
    private ContentChannelItemAssociation() { }

    public ContentChannelItemAssociation(
        ContentChannelItemAssociationId id,
        ContentChannelItemId parentItemId,
        ContentChannelItemId childItemId,
        int order
    )
        : base(id)
    {
        ParentItemId = parentItemId;
        ChildItemId = childItemId;
        Order = order;
    }

    public ContentChannelItemId ParentItemId { get; private set; }
    public ContentChannelItemId ChildItemId { get; private set; }
    public int Order { get; private set; }
}
