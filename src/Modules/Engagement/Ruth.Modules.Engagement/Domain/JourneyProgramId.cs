using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Engagement.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a JourneyProgram.</summary>
public readonly record struct JourneyProgramId(Guid Value) : IStronglyTypedId<JourneyProgramId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static JourneyProgramId Empty => new(Guid.Empty);
    public static JourneyProgramId Create() => new(StronglyTypedId.NextGuid());
    public static JourneyProgramId From(Guid value) => new(value);
    public override string ToString() => Value.ToString("D");
}
