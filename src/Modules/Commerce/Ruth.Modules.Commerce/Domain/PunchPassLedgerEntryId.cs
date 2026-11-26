using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a PunchPassLedgerEntry.</summary>
public readonly record struct PunchPassLedgerEntryId(Guid Value)
    : IStronglyTypedId<PunchPassLedgerEntryId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static PunchPassLedgerEntryId Empty => new(Guid.Empty);

    public static PunchPassLedgerEntryId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static PunchPassLedgerEntryId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
