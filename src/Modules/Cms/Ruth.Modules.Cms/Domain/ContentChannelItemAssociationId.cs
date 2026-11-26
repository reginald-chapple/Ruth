using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Cms.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for a ContentChannelItemAssociation.</summary>
public readonly record struct ContentChannelItemAssociationId(Guid Value)
    : IStronglyTypedId<ContentChannelItemAssociationId>
{
    public bool IsEmpty => Value == Guid.Empty;
    public static ContentChannelItemAssociationId Empty => new(Guid.Empty);

    public static ContentChannelItemAssociationId Create()
    {
        return new(StronglyTypedId.NextGuid());
    }

    public static ContentChannelItemAssociationId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
