using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.BulkCommunications;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationTemplate" />.</summary>
public readonly record struct CommunicationTemplateId(Guid Value)
    : IStronglyTypedId<CommunicationTemplateId>
{
    public static CommunicationTemplateId Empty => new(Guid.Empty);

    public static CommunicationTemplateId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationTemplateId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
