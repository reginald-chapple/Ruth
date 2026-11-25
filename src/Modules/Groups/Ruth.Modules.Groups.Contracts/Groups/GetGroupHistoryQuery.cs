using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Retrieves point-in-time historical snapshots for a Group.</summary>
public sealed record GetGroupHistoryQuery(Guid GroupId)
    : IQuery<IReadOnlyList<GroupHistoricalResponse>>;
