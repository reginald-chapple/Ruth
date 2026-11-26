using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningGradingScaleId(Guid Value)
    : IStronglyTypedId<LearningGradingScaleId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningGradingScaleId Empty => new(Guid.Empty);

    public static LearningGradingScaleId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningGradingScaleId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
