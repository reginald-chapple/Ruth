using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningGradingSystemId(Guid Value)
    : IStronglyTypedId<LearningGradingSystemId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningGradingSystemId Empty => new(Guid.Empty);

    public static LearningGradingSystemId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningGradingSystemId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
