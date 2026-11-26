using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a SupportRequestComment.</summary>
public readonly record struct SupportRequestCommentId(Guid Value)
    : IStronglyTypedId<SupportRequestCommentId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static SupportRequestCommentId Empty => new(Guid.Empty);

    public static SupportRequestCommentId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static SupportRequestCommentId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
