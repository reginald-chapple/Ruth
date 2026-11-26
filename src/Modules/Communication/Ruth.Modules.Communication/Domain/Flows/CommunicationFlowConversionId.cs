using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Communication.Domain.Flows;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="CommunicationFlowConversion" />.</summary>
public readonly record struct CommunicationFlowConversionId(Guid Value)
    : IStronglyTypedId<CommunicationFlowConversionId>
{
    public static CommunicationFlowConversionId Empty => new(Guid.Empty);

    public static CommunicationFlowConversionId Create()
    {
        return new(Guid.CreateVersion7());
    }

    public static CommunicationFlowConversionId From(Guid value)
    {
        return new(value);
    }

    public override string ToString()
    {
        return Value.ToString("D");
    }
}
