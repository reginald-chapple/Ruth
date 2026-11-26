using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningCourseId(Guid Value) : IStronglyTypedId<LearningCourseId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningCourseId Empty => new(Guid.Empty);

    public static LearningCourseId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningCourseId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
