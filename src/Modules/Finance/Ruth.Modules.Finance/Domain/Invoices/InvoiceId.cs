using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Invoices;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="Invoice" />.</summary>
public readonly record struct InvoiceId(Guid Value) : IStronglyTypedId<InvoiceId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static InvoiceId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static InvoiceId Create()
    {
        return new InvoiceId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static InvoiceId From(Guid value)
    {
        return new InvoiceId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
