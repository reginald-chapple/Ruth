using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="ProcessedGatewayWebhook" />.</summary>
public readonly record struct ProcessedGatewayWebhookId(Guid Value)
    : IStronglyTypedId<ProcessedGatewayWebhookId>
{
    /// <inheritdoc />
    public static ProcessedGatewayWebhookId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static ProcessedGatewayWebhookId Create()
    {
        return new ProcessedGatewayWebhookId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static ProcessedGatewayWebhookId From(Guid value)
    {
        return new ProcessedGatewayWebhookId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
