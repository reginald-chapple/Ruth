namespace Ruth.SharedKernel.Primitives;

/// <summary>
/// Contract for strongly-typed UUIDv7 entity identifiers implemented as readonly record structs.
/// </summary>
/// <typeparam name="TSelf">The implementing identifier value type.</typeparam>
public interface IStronglyTypedId<TSelf> : IEquatable<TSelf>
    where TSelf : struct, IStronglyTypedId<TSelf>
{
    /// <summary>Gets the underlying <see cref="Guid" /> value.</summary>
    Guid Value { get; }

    /// <summary>Gets a value indicating whether the identifier represents <see cref="Guid.Empty" />.</summary>
    bool IsEmpty => Value == Guid.Empty;

    /// <summary>Gets an empty identifier wrapping <see cref="Guid.Empty" />.</summary>
    static abstract TSelf Empty { get; }

    /// <summary>Creates a new identifier backed by a monotonic UUIDv7 value.</summary>
    static abstract TSelf Create();

    /// <summary>Wraps an existing <see cref="Guid" /> into the strongly-typed identifier.</summary>
    /// <param name="value">The underlying identifier value.</param>
    static abstract TSelf From(Guid value);
}
