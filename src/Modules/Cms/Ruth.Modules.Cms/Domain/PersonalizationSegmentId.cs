using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PersonalizationSegment.</summary>
public readonly record struct PersonalizationSegmentId(Guid Value)
    : IStronglyTypedId<PersonalizationSegmentId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PersonalizationSegmentId Empty => new(Guid.Empty);

    public static PersonalizationSegmentId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PersonalizationSegmentId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
