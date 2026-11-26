using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Gateways;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="FinancialGateway" />.</summary>
public readonly record struct FinancialGatewayId(Guid Value) : IStronglyTypedId<FinancialGatewayId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static FinancialGatewayId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static FinancialGatewayId Create()
    {
        return new FinancialGatewayId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static FinancialGatewayId From(Guid value)
    {
        return new FinancialGatewayId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
