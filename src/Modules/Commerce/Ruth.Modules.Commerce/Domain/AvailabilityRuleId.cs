using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for an AvailabilityRule.</summary>
public readonly record struct AvailabilityRuleId(Guid Value) : IStronglyTypedId<AvailabilityRuleId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static AvailabilityRuleId Empty => new(Guid.Empty);

    public static AvailabilityRuleId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static AvailabilityRuleId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
