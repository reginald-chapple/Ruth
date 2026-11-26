using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ConstituentContact.</summary>
public readonly record struct ConstituentContactId(Guid Value)
    : IStronglyTypedId<ConstituentContactId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ConstituentContactId Empty => new(Guid.Empty);
    public static ConstituentContactId Create() => new(StronglyTypedId.NextGuid());
    public static ConstituentContactId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
