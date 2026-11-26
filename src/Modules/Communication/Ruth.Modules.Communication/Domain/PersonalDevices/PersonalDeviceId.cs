using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.PersonalDevices;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="PersonalDevice" />.</summary>
public readonly record struct PersonalDeviceId(Guid Value) : IStronglyTypedId<PersonalDeviceId>
{
    public static PersonalDeviceId Empty => new(Guid.Empty);

    public static PersonalDeviceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static PersonalDeviceId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
