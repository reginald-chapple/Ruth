using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Member record inside an <see cref="EntitySet" /> bulk selection.</summary>
public sealed class EntitySetItem : Entity<Guid>
{
    private EntitySetItem() { }

    internal EntitySetItem(
        Guid id,
        EntitySetId entitySetId,
        Guid entityId,
        int order,
        string? additionalMergeValuesJson = null
    )
        : base(id)
    {
        EntitySetId = entitySetId;
        EntityId = entityId;
        Order = Math.Max(0, order);
        AdditionalMergeValuesJson = additionalMergeValuesJson;
    }

    /// <summary>Gets the owning entity set identifier.</summary>
    public EntitySetId EntitySetId { get; private set; }

    /// <summary>Gets the selected entity identifier.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the display order within the set.</summary>
    public int Order { get; private set; }

    /// <summary>Gets optional JSON merge metadata attached to this item.</summary>
    public string? AdditionalMergeValuesJson { get; private set; }
}
