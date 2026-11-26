using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningTermId(Guid Value) : IStronglyTypedId<LearningTermId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningTermId Empty => new(Guid.Empty);

    public static LearningTermId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningTermId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
