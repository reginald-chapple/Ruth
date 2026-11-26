using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Lms.Domain;

public readonly record struct LearningParticipantId(Guid Value)
    : IStronglyTypedId<LearningParticipantId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static LearningParticipantId Empty => new(Guid.Empty);

    public static LearningParticipantId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static LearningParticipantId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
