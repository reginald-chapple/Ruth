using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="RecurringScheduleTransaction" />.</summary>
public readonly record struct RecurringScheduleTransactionId(Guid Value)
    : IStronglyTypedId<RecurringScheduleTransactionId>
{
    /// <inheritdoc />
    public bool IsEmpty => Value == Guid.Empty;

    /// <inheritdoc />
    public static RecurringScheduleTransactionId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static RecurringScheduleTransactionId Create()
    {
        return new RecurringScheduleTransactionId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static RecurringScheduleTransactionId From(Guid value)
    {
        return new RecurringScheduleTransactionId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
