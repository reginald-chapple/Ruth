using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="NoteWatch" />.</summary>
public readonly record struct NoteWatchId(Guid Value) : IStronglyTypedId<NoteWatchId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static NoteWatchId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 note watch identifier.</summary>
    public static NoteWatchId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static NoteWatchId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
