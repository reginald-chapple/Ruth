using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.PhoneNumbers;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SystemPhoneNumber" />.</summary>
public readonly record struct SystemPhoneNumberId(Guid Value)
    : IStronglyTypedId<SystemPhoneNumberId>
{
    public static SystemPhoneNumberId Empty => new(Guid.Empty);

    public static SystemPhoneNumberId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static SystemPhoneNumberId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
