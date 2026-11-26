using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Retrieves a transactional system communication template by identifier.</summary>
/// <param name="SystemCommunicationId">Target system communication identifier.</param>
public sealed record GetSystemCommunicationByIdQuery(Guid SystemCommunicationId)
    : IQuery<SystemCommunicationResponse>;
