namespace Ruth.SharedKernel.Primitives;

/// <summary>
/// Factory and parsing helpers for <see cref="IStronglyTypedId{TSelf}" /> backed by UUIDv7 values.
/// </summary>
public static class StronglyTypedId
{
    /// <summary>Generates a new time-ordered UUIDv7 value.</summary>
    public static Guid NextGuid()
    {
        return Guid.CreateVersion7();
    }

    /// <summary>Generates a new time-ordered UUIDv7 value for a specific timestamp.</summary>
    /// <param name="timestamp">The timestamp embedded in the UUIDv7 prefix.</param>
    public static Guid NextGuid(DateTimeOffset timestamp)
    {
        return Guid.CreateVersion7(timestamp);
    }

    /// <summary>Creates a new strongly-typed identifier via <see cref="IStronglyTypedId{TSelf}.Create" />.</summary>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    public static TId Create<TId>()
        where TId : struct, IStronglyTypedId<TId>
    {
        return TId.Create();
    }

    /// <summary>Creates a strongly-typed identifier from a UUIDv7 seeded with <paramref name="timestamp" />.</summary>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    /// <param name="timestamp">The timestamp embedded in the UUIDv7 prefix.</param>
    public static TId Create<TId>(DateTimeOffset timestamp)
        where TId : struct, IStronglyTypedId<TId>
    {
        return TId.From(Guid.CreateVersion7(timestamp));
    }

    /// <summary>Compares two strongly-typed identifiers by their underlying UUIDv7 ordering.</summary>
    public static int Compare<TId>(TId left, TId right)
        where TId : struct, IStronglyTypedId<TId>
    {
        return left.Value.CompareTo(right.Value);
    }

    /// <summary>Parses a character span into a strongly-typed identifier without intermediate allocations.</summary>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    /// <param name="input">The raw GUID text.</param>
    /// <param name="id">The parsed strongly-typed identifier, or <see cref="IStronglyTypedId{TSelf}.Empty" />.</param>
    public static bool TryParse<TId>(ReadOnlySpan<char> input, out TId id)
        where TId : struct, IStronglyTypedId<TId>
    {
        if (Guid.TryParse(input, out var guid))
        {
            id = TId.From(guid);
            return true;
        }

        id = TId.Empty;
        return false;
    }

    /// <summary>Parses a string into a strongly-typed identifier.</summary>
    /// <typeparam name="TId">The strongly-typed identifier type.</typeparam>
    /// <param name="input">The raw GUID text.</param>
    /// <param name="id">The parsed strongly-typed identifier, or <see cref="IStronglyTypedId{TSelf}.Empty" />.</param>
    public static bool TryParse<TId>(string? input, out TId id)
        where TId : struct, IStronglyTypedId<TId>
    {
        if (input is null)
        {
            id = TId.Empty;
            return false;
        }

        return TryParse(input.AsSpan(), out id);
    }
}
