using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningActivityId(Guid Value) : IStronglyTypedId<LearningActivityId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningActivityId Empty => new(Guid.Empty);

    public static LearningActivityId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningActivityId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
