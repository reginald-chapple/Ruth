using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="IssuedWalletPass" />.</summary>
public readonly record struct IssuedWalletPassId(Guid Value) : IStronglyTypedId<IssuedWalletPassId>
{
    /// <inheritdoc />
    public static IssuedWalletPassId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static IssuedWalletPassId Create()
    {
        return new IssuedWalletPassId(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static IssuedWalletPassId From(Guid value)
    {
        return new IssuedWalletPassId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
