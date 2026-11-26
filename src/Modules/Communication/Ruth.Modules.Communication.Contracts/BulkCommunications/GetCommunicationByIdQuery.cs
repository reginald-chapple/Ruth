using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Retrieves full detail and recipient statuses for a single Communication.</summary>
/// <param name="CommunicationId">Unique communication identifier.</param>
public sealed record GetCommunicationByIdQuery(Guid CommunicationId)
    : IQuery<CommunicationDetailResponse>;
