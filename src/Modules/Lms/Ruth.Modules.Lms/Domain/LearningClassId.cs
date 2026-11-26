using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningClassId(Guid Value) : IStronglyTypedId<LearningClassId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningClassId Empty => new(Guid.Empty);

    public static LearningClassId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningClassId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
