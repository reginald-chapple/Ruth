using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Invoices;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="InvoiceLineItem" />.</summary>
public readonly record struct InvoiceLineItemId(Guid Value) : IStronglyTypedId<InvoiceLineItemId>
{
    /// <inheritdoc />
    public static InvoiceLineItemId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static InvoiceLineItemId Create()
    {
        return new InvoiceLineItemId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static InvoiceLineItemId From(Guid value)
    {
        return new InvoiceLineItemId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
