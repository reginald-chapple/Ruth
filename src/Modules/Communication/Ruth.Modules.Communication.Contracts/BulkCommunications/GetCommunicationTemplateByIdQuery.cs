using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Retrieves a single CommunicationTemplate by identifier.</summary>
/// <param name="TemplateId">Unique template identifier.</param>
public sealed record GetCommunicationTemplateByIdQuery(Guid TemplateId)
    : IQuery<CommunicationTemplateResponse>;
