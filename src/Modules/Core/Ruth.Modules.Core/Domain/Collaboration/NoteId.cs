using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Note" />.</summary>
public readonly record struct NoteId(Guid Value) : IStronglyTypedId<NoteId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static NoteId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 note identifier.</summary>
    public static NoteId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static NoteId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
