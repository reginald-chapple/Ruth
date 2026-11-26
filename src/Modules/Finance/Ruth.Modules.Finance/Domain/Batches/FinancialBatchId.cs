using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Batches;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialBatch" />.</summary>
public readonly record struct FinancialBatchId(Guid Value) : IStronglyTypedId<FinancialBatchId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FinancialBatchId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialBatchId Create()
    {
        return new FinancialBatchId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialBatchId From(Guid value)
    {
        return new FinancialBatchId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
