using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Labels.CheckInLabel" />.</summary>
public readonly record struct CheckInLabelId(Guid Value) : IStronglyTypedId<CheckInLabelId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static CheckInLabelId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 check-in label identifier.</summary>
    public static CheckInLabelId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static CheckInLabelId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
