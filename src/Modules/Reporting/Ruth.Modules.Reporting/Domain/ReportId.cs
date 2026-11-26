using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a Report.</summary>
public readonly record struct ReportId(Guid Value) : IStronglyTypedId<ReportId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ReportId Empty => new(Guid.Empty);

    public static ReportId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ReportId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
