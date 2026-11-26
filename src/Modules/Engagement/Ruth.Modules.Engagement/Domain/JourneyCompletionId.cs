using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a JourneyCompletion.</summary>
public readonly record struct JourneyCompletionId(Guid Value)
    : IStronglyTypedId<JourneyCompletionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static JourneyCompletionId Empty => new(Guid.Empty);

    public static JourneyCompletionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static JourneyCompletionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
