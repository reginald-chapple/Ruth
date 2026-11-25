using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Lists members of a group with optional status and archive filters.</summary>
public sealed record GetGroupMembersQuery(
    Guid GroupId,
    GroupMemberStatus? Status = null,
    bool IncludeArchived = false
) : IQuery<IReadOnlyList<GroupMemberResponse>>;
