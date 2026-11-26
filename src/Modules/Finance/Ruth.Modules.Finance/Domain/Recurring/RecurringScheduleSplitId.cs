using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Finance.Domain.Recurring;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="RecurringScheduleSplit" />.</summary>
public readonly record struct RecurringScheduleSplitId(Guid Value)
    : IStronglyTypedId<RecurringScheduleSplitId>
{
    /// <inheritdoc />
    public static RecurringScheduleSplitId Empty => new(Guid.Empty);

    /// <inheritdoc />
    public static RecurringScheduleSplitId Create()
    {
        return new RecurringScheduleSplitId(StronglyTypedId.NextGuid());
    }

    /// <inheritdoc />
    public static RecurringScheduleSplitId From(Guid value)
    {
        return new RecurringScheduleSplitId(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString("D");
    }
}
