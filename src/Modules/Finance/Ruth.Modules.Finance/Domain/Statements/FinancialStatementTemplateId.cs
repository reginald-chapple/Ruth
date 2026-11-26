using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Statements;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialStatementTemplate" />.</summary>
public readonly record struct FinancialStatementTemplateId(Guid Value)
    : IStronglyTypedId<FinancialStatementTemplateId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FinancialStatementTemplateId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialStatementTemplateId Create()
    {
        return new FinancialStatementTemplateId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialStatementTemplateId From(Guid value)
    {
        return new FinancialStatementTemplateId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
