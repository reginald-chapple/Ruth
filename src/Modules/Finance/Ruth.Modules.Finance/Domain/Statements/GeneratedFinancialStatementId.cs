using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Statements;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="GeneratedFinancialStatement" />.</summary>
public readonly record struct GeneratedFinancialStatementId(Guid Value)
    : IStronglyTypedId<GeneratedFinancialStatementId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static GeneratedFinancialStatementId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static GeneratedFinancialStatementId Create()
    {
        return new GeneratedFinancialStatementId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static GeneratedFinancialStatementId From(Guid value)
    {
        return new GeneratedFinancialStatementId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
