using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Association tagging an entity with a <see cref="Tag" />.</summary>
public sealed class TaggedItem : Entity<Guid>, IAuditableEntity
{
    private TaggedItem() { }

    internal TaggedItem(
        Guid id,
        TagId tagId,
        EntityTypeDescriptorId entityTypeId,
        Guid entityId,
        int quantity
    )
        : base(id)
    {
        TagId = tagId;
        EntityTypeId = entityTypeId;
        EntityId = entityId;
        Quantity = Math.Max(1, quantity);
    }

    /// <summary>Gets the applied tag identifier.</summary>
    public TagId TagId { get; private set; }

    /// <summary>Gets the tagged entity type descriptor identifier.</summary>
    public EntityTypeDescriptorId EntityTypeId { get; private set; }

    /// <summary>Gets the tagged entity identifier.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the optional quantity weight.</summary>
    public int Quantity { get; private set; } = 1;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }
}
