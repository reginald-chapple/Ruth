using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Strongly-typed UUIDv7 identifier for a <see cref="GroupDemographicType" />.</summary>
public readonly record struct GroupDemographicTypeId(Guid Value)
    : IStronglyTypedId<GroupDemographicTypeId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static GroupDemographicTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 identifier.</summary>
    public static GroupDemographicTypeId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static GroupDemographicTypeId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
