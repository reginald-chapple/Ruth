using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a SupportRequest.</summary>
public readonly record struct SupportRequestId(Guid Value) : IStronglyTypedId<SupportRequestId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static SupportRequestId Empty => new(Guid.Empty);
    public static SupportRequestId Create() => new(StronglyTypedId.NextGuid());
    public static SupportRequestId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
