using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningActivityCompletionId(Guid Value)
    : IStronglyTypedId<LearningActivityCompletionId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningActivityCompletionId Empty => new(Guid.Empty);

    public static LearningActivityCompletionId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningActivityCompletionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
