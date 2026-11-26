using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningClassAnnouncementId(Guid Value)
    : IStronglyTypedId<LearningClassAnnouncementId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningClassAnnouncementId Empty => new(Guid.Empty);

    public static LearningClassAnnouncementId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningClassAnnouncementId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
