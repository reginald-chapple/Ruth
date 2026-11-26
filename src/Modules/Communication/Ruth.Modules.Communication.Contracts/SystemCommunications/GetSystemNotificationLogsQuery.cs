using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.SystemCommunications;

/// <summary>Lists recent transactional system notification delivery audit logs.</summary>
/// <param name="SystemCommunicationKey">Optional system communication key filter.</param>
/// <param name="Channel">Optional channel filter.</param>
/// <param name="Limit">Maximum records to return (1–200).</param>
public sealed record GetSystemNotificationLogsQuery(
    string? SystemCommunicationKey = null,
    CommunicationChannel? Channel = null,
    int Limit = 50
) : IQuery<IReadOnlyList<SystemNotificationLogResponse>>;
