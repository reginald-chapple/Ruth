using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain.WalletPasses;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="WalletPassDeviceRegistration" />.</summary>
public readonly record struct WalletPassDeviceRegistrationId(Guid Value)
    : IStronglyTypedId<WalletPassDeviceRegistrationId>
{
    /// <inheritdoc />
    public static WalletPassDeviceRegistrationId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static WalletPassDeviceRegistrationId Create()
    {
        return new WalletPassDeviceRegistrationId(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static WalletPassDeviceRegistrationId From(Guid value)
    {
        return new WalletPassDeviceRegistrationId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
