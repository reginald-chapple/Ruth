using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Lists bulk and targeted Communication campaigns ordered newest first.</summary>
/// <param name="Status">Optional lifecycle status filter.</param>
/// <param name="Channel">Optional channel filter.</param>
/// <param name="Limit">Maximum records to return (1..200).</param>
public sealed record GetCommunicationsQuery(
    CommunicationStatus? Status = null,
    CommunicationChannel? Channel = null,
    int Limit = 50
) : IQuery<IReadOnlyList<CommunicationSummaryResponse>>;
