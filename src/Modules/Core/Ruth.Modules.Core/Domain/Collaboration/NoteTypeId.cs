using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="NoteType" />.</summary>
public readonly record struct NoteTypeId(Guid Value) : IStronglyTypedId<NoteTypeId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static NoteTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 note type identifier.</summary>
    public static NoteTypeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static NoteTypeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
