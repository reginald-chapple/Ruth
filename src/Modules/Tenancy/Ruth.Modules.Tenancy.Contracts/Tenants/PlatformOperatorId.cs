using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Strongly-typed identifier for an internal SaaS platform operator account.</summary>
/// <param name="Value">The underlying UUIDv7 value.</param>
public readonly record struct PlatformOperatorId(Guid Value) : IStronglyTypedId<PlatformOperatorId>
{
    /// <summary>Gets the empty <see cref="PlatformOperatorId" />.</summary>
    public static PlatformOperatorId Empty => new(Guid.Empty);

    /// <summary>Creates a new time-ordered <see cref="PlatformOperatorId" />.</summary>
    public static PlatformOperatorId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing <see cref="Guid" />.</summary>
    public static PlatformOperatorId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
