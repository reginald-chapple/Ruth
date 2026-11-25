using Ruth.Modules.Groups.Contracts.Members;

namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Configured GroupSync rule binding a Group+Role to an external person source.</summary>
public sealed record GroupSyncRuleResponse(
    Guid Id,
    Guid GroupId,
    Guid GroupTypeRoleId,
    GroupSyncSourceType SourceType,
    Guid? SourceReferenceId,
    string? SourceFilterValue,
    int? ScheduleIntervalMinutes,
    bool AddMissingMembers,
    bool RemoveExtraMembers,
    GroupMemberStatus TargetMemberStatus,
    DateTimeOffset? LastSyncOnUtc,
    int LastAddedCount,
    int LastRemovedCount
);
