using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Reporting.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ReportField.</summary>
public readonly record struct ReportFieldId(Guid Value) : IStronglyTypedId<ReportFieldId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ReportFieldId Empty => new(Guid.Empty);

    public static ReportFieldId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ReportFieldId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
