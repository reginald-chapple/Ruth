using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain.Collaboration;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="HistoryLogEntry" />.</summary>
public readonly record struct HistoryLogEntryId(Guid Value) : IStronglyTypedId<HistoryLogEntryId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static HistoryLogEntryId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 history log entry identifier.</summary>
    public static HistoryLogEntryId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static HistoryLogEntryId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
