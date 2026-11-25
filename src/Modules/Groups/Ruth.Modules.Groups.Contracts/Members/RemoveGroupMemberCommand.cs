using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Members;

/// <summary>Archives (soft-deletes) or removes a GroupMember.</summary>
public sealed record RemoveGroupMemberCommand(
    Guid GroupMemberId,
    bool ArchiveInsteadOfDelete = true,
    Guid? ArchivedByPersonId = null
) : ICommand<bool>;
