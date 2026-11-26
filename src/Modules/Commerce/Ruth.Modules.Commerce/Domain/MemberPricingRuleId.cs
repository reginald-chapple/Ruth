using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a MemberPricingRule.</summary>
public readonly record struct MemberPricingRuleId(Guid Value)
    : IStronglyTypedId<MemberPricingRuleId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static MemberPricingRuleId Empty => new(Guid.Empty);

    public static MemberPricingRuleId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static MemberPricingRuleId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
