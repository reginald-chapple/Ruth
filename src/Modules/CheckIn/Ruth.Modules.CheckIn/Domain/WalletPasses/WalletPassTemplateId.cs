using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="WalletPassTemplate" />.</summary>
public readonly record struct WalletPassTemplateId(Guid Value)
    : IStronglyTypedId<WalletPassTemplateId>
{
    /// <inheritdoc />
    public static WalletPassTemplateId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static WalletPassTemplateId Create()
    {
        return new WalletPassTemplateId(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static WalletPassTemplateId From(Guid value)
    {
        return new WalletPassTemplateId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
