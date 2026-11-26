using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningCourseRequirementId(Guid Value)
    : IStronglyTypedId<LearningCourseRequirementId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningCourseRequirementId Empty => new(Guid.Empty);

    public static LearningCourseRequirementId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningCourseRequirementId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
