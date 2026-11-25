using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="PersonPreference" />.</summary>
public readonly record struct PersonPreferenceId(Guid Value) : IStronglyTypedId<PersonPreferenceId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static PersonPreferenceId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 person preference identifier.</summary>
    public static PersonPreferenceId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static PersonPreferenceId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
