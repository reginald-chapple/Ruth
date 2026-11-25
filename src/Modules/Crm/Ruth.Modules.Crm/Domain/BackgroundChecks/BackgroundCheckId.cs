using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.BackgroundChecks;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="BackgroundCheck" /> aggregate.</summary>
public readonly record struct BackgroundCheckId(Guid Value) : IStronglyTypedId<BackgroundCheckId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static BackgroundCheckId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 background check identifier.</summary>
    public static BackgroundCheckId Create()
    {
        return new BackgroundCheckId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static BackgroundCheckId From(Guid value)
    {
        return new BackgroundCheckId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
