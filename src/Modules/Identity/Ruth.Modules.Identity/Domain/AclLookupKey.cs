namespace Ruth.Modules.Identity.Domain;

/// <summary>Case-insensitive lookup key identifying an entity's ACL rule bucket for a specific action.</summary>
public readonly record struct AclLookupKey : IEquatable<AclLookupKey>
{
    /// <summary>Initializes a normalized lookup key.</summary>
    public AclLookupKey(string entityTypeKey, Guid entityId, string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityTypeKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        EntityTypeKey = entityTypeKey.Trim();
        EntityId = entityId;
        Action = action.Trim();
    }

    /// <summary>Gets the canonical entity type key.</summary>
    public string EntityTypeKey { get; }

    /// <summary>Gets the entity instance identifier.</summary>
    public Guid EntityId { get; }

    /// <summary>Gets the action name.</summary>
    public string Action { get; }

    /// <inheritdoc />
    public bool Equals(AclLookupKey other)
    {
        return EntityId == other.EntityId
            && string.Equals(EntityTypeKey, other.EntityTypeKey, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Action, other.Action, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(EntityTypeKey),
            EntityId,
            StringComparer.OrdinalIgnoreCase.GetHashCode(Action)
        );
    }
}
