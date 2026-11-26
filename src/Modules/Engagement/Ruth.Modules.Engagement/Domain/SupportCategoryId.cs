using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a SupportCategory.</summary>
public readonly record struct SupportCategoryId(Guid Value) : IStronglyTypedId<SupportCategoryId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static SupportCategoryId Empty => new(Guid.Empty);

    public static SupportCategoryId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static SupportCategoryId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
