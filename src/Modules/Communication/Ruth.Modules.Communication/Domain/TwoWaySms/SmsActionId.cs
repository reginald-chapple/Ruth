using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.TwoWaySms;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="SmsAction" />.</summary>
public readonly record struct SmsActionId(Guid Value) : IStronglyTypedId<SmsActionId>
{
    public static SmsActionId Empty => new(Guid.Empty);

    public static SmsActionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static SmsActionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
