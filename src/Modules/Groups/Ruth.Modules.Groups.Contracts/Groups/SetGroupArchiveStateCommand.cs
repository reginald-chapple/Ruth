using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Groups;

/// <summary>Archives (soft-deletes) or unarchives a Group.</summary>
public sealed record SetGroupArchiveStateCommand(
    Guid GroupId,
    bool IsArchived,
    Guid? ArchivedByPersonId = null
) : ICommand<GroupDetailResponse>;
