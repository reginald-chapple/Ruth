using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Tenancy.Contracts.Tenants;

/// <summary>Strongly-typed identifier for a tenant's Stripe Connect Express onboarding account record.</summary>
/// <param name="Value">The underlying UUIDv7 value.</param>
public readonly record struct StripeConnectAccountId(Guid Value)
    : IStronglyTypedId<StripeConnectAccountId>
{
    /// <summary>Gets the empty <see cref="StripeConnectAccountId" />.</summary>
    public static StripeConnectAccountId Empty => new(Guid.Empty);

    /// <summary>Creates a new time-ordered <see cref="StripeConnectAccountId" />.</summary>
    public static StripeConnectAccountId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing <see cref="Guid" />.</summary>
    public static StripeConnectAccountId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
