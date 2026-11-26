using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningClassContentPageId(Guid Value)
    : IStronglyTypedId<LearningClassContentPageId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningClassContentPageId Empty => new(Guid.Empty);

    public static LearningClassContentPageId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningClassContentPageId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
