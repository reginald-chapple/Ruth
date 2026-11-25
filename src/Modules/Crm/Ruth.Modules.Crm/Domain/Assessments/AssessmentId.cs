using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Assessments;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="Assessment" /> aggregate.</summary>
public readonly record struct AssessmentId(Guid Value) : IStronglyTypedId<AssessmentId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static AssessmentId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 assessment identifier.</summary>
    public static AssessmentId Create()
    {
        return new AssessmentId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static AssessmentId From(Guid value)
    {
        return new AssessmentId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
