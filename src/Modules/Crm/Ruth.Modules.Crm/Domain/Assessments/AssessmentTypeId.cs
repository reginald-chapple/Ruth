using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Crm.Domain.Assessments;

/// <summary>Strongly-typed UUIDv7 identifier for an <see cref="AssessmentType" /> aggregate.</summary>
public readonly record struct AssessmentTypeId(Guid Value) : IStronglyTypedId<AssessmentTypeId>
{
    /// <summary>Gets the empty identifier sentinel.</summary>
    public static AssessmentTypeId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 assessment type identifier.</summary>
    public static AssessmentTypeId Create()
    {
        return new AssessmentTypeId(Guid.CreateVersion7());
    }

    /// <summary>Wraps an existing GUID value.</summary>
    public static AssessmentTypeId From(Guid value)
    {
        return new AssessmentTypeId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
