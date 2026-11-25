using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Signals;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="SignalType" /> aggregate.</summary>
public readonly record struct SignalTypeId(Guid Value) : IStronglyTypedId<SignalTypeId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static SignalTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 signal type identifier.</summary>
    public static SignalTypeId Create()
    {
        return new SignalTypeId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static SignalTypeId From(Guid value)
    {
        return new SignalTypeId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
