using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Lists all groups a person belongs to.</summary>
public sealed record GetPersonGroupMembershipsQuery(
    Guid PersonId,
    bool ActiveOnly = true,
    bool IncludeArchived = false
) : IQuery<IReadOnlyList<GroupMemberResponse>>;
