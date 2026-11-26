using System.Runtime.CompilerServices;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.SharedKernel.Primitives;

/// <summary>
/// Base type for DDD entities with identity-based equality and tenant ownership.
/// </summary>
/// <typeparam name="TId">The strongly-typed entity identifier.</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>, ITenantScoped
    where TId : notnull, IEquatable<TId>
{
    /// <summary>Initializes an entity for persistence materialization.</summary>
    protected Entity()
    {
        Id = default!;
    }

    /// <summary>Initializes an entity with a non-default strongly-typed identifier.</summary>
    /// <param name="id">The entity identifier.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id" /> is the default value.</exception>
    protected Entity(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (EqualityComparer<TId>.Default.Equals(id, default))
        {
            throw new ArgumentException(
                "Entity identifier cannot be empty or default.",
                nameof(id)
            );
        }

        Id = id;
    }

    /// <summary>Gets the strongly-typed identifier for this entity.</summary>
    public TId Id { get; protected init; }

    /// <inheritdoc />
    public TenantId TenantId { get; private set; }

    /// <summary>Gets a value indicating whether this entity has not yet been assigned an identifier.</summary>
    public bool IsTransient => EqualityComparer<TId>.Default.Equals(Id, default);

    /// <inheritdoc />
    public void AssignTenant(TenantId tenantId)
    {
        if (tenantId.IsEmpty)
        {
            throw new ArgumentException("Tenant identifier cannot be empty.", nameof(tenantId));
        }

        if (!TenantId.IsEmpty && TenantId != tenantId)
        {
            throw new CrossTenantAccessException(
                $"Cannot reassign entity '{GetType().Name}' ({Id}) from tenant '{TenantId}' to '{tenantId}'."
            );
        }

        TenantId = tenantId;
    }

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other)
    {
        return other is not null
            && (
                ReferenceEquals(this, other)
                || (
                    GetType() == other.GetType()
                    && !IsTransient
                    && !other.IsTransient
                    && EqualityComparer<TId>.Default.Equals(Id, other.Id)
                )
            );
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Entity<TId> other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return IsTransient ? RuntimeHelpers.GetHashCode(this) : HashCode.Combine(GetType(), Id);
    }

    /// <summary>Determines whether two entities represent the same identity.</summary>
    public static bool operator ==(Entity<TId>? left, Entity<TId>? right)
    {
        return Equals(left, right);
    }

    /// <summary>Determines whether two entities do not represent the same identity.</summary>
    public static bool operator !=(Entity<TId>? left, Entity<TId>? right)
    {
        return !Equals(left, right);
    }
}
