using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupDemographicValue" />.</summary>
public readonly record struct GroupDemographicValueId(Guid Value)
    : IStronglyTypedId<GroupDemographicValueId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupDemographicValueId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupDemographicValueId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupDemographicValueId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
