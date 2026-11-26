using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningProgramId(Guid Value) : IStronglyTypedId<LearningProgramId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningProgramId Empty => new(Guid.Empty);

    public static LearningProgramId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningProgramId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
