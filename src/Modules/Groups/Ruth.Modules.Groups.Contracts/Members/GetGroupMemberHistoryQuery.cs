using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Retrieves point-in-time historical snapshots for a GroupMember.</summary>
public sealed record GetGroupMemberHistoryQuery(Guid GroupMemberId)
    : IQuery<IReadOnlyList<GroupMemberHistoricalResponse>>;
