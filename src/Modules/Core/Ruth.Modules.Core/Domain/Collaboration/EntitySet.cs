using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Ephemeral (expiring) or saved bulk selection set of entity identifiers.</summary>
public sealed class EntitySet : AggregateRoot<EntitySetId>, IAuditableEntity
{
    private readonly List<EntitySetItem> _items = [];

    private EntitySet() { }

    private EntitySet(
        EntitySetId id,
        EntitySetId? parentEntitySetId,
        string? name,
        EntityTypeDescriptorId? entityTypeId,
        DateTimeOffset? expireOnUtc,
        Guid? purposeValueId,
        string? note,
        int order
    )
        : base(id)
    {
        ParentEntitySetId = parentEntitySetId;
        Name = name;
        EntityTypeId = entityTypeId;
        ExpireOnUtc = expireOnUtc;
        PurposeValueId = purposeValueId;
        Note = note;
        Order = Math.Max(0, order);
    }

    /// <summary>Gets the optional parent entity set identifier.</summary>
    public EntitySetId? ParentEntitySetId { get; private set; }

    /// <summary>Gets the optional name (saved sets are typically named; ephemeral sets may be unnamed).</summary>
    public string? Name { get; private set; }

    /// <summary>Gets the optional entity type descriptor identifier for all set members.</summary>
    public EntityTypeDescriptorId? EntityTypeId { get; private set; }

    /// <summary>Gets the optional expiration timestamp (null = permanent/saved set).</summary>
    public DateTimeOffset? ExpireOnUtc { get; private set; }

    /// <summary>Gets the optional purpose defined value identifier.</summary>
    public Guid? PurposeValueId { get; private set; }

    /// <summary>Gets optional notes describing this selection set.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets the display order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets the ordered entity set items.</summary>
    public IReadOnlyCollection<EntitySetItem> Items => _items.AsReadOnly();

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

    /// <summary>Returns whether this entity set has expired as of <paramref name="nowUtc" />.</summary>
    public bool IsExpiredAt(DateTimeOffset nowUtc)
    {
        return ExpireOnUtc.HasValue && ExpireOnUtc.Value <= nowUtc;
    }

    /// <summary>Creates a new ephemeral or saved entity set populated with distinct entity IDs.</summary>
    public static Result<EntitySet> Create(
        string? name,
        EntityTypeDescriptorId? entityTypeId,
        IReadOnlyList<Guid> entityIds,
        int? expireInMinutes,
        DateTimeOffset nowUtc,
        EntitySetId? parentEntitySetId = null,
        Guid? purposeValueId = null,
        string? note = null,
        int order = 0
    )
    {
        var nameResult = CoreTextPolicy.ValidateOptionalText(
            name,
            CoreTextPolicy.NameMaxLength,
            allowNewLines: false
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure<EntitySet>(nameResult.Errors);
        }

        DateTimeOffset? expireOnUtc = expireInMinutes is > 0
            ? nowUtc.AddMinutes(expireInMinutes.Value)
            : null;

        var set = new EntitySet(
            EntitySetId.Create(),
            parentEntitySetId,
            nameResult.Value,
            entityTypeId,
            expireOnUtc,
            purposeValueId,
            string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            order
        );

        var idx = 0;
        foreach (var entityId in entityIds.Distinct())
        {
            if (entityId != Guid.Empty)
            {
                set._items.Add(new EntitySetItem(Guid.CreateVersion7(), set.Id, entityId, idx++));
            }
        }

        return Result.Success(set);
    }
}
