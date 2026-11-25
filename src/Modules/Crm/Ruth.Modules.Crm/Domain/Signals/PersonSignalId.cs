using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Signals;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="PersonSignal" /> entity.</summary>
public readonly record struct PersonSignalId(Guid Value) : IStronglyTypedId<PersonSignalId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static PersonSignalId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 person signal identifier.</summary>
    public static PersonSignalId Create()
    {
        return new PersonSignalId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static PersonSignalId From(Guid value)
    {
        return new PersonSignalId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
