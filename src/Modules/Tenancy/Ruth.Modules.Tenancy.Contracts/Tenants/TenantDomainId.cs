using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Strongly-typed identifier for a tenant domain mapping.</summary>
/// <param name="Value">The underlying UUIDv7 value.</param>
public readonly record struct TenantDomainId(Guid Value) : IStronglyTypedId<TenantDomainId>
{
    /// <summary>Gets the empty <see cref="TenantDomainId" />.</summary>
    public static TenantDomainId Empty => new(Guid.Empty);

    /// <summary>Creates a new time-ordered <see cref="TenantDomainId" />.</summary>
    public static TenantDomainId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing <see cref="Guid" />.</summary>
    public static TenantDomainId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
