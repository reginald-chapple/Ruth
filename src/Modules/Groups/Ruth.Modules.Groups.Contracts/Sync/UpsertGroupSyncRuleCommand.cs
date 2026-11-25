using Ruth.Modules.Groups.Contracts.Members;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Groups.Contracts.Sync;

/// <summary>Creates or updates a GroupSync rule for a Group and Role.</summary>
public sealed record UpsertGroupSyncRuleCommand(
    Guid GroupId,
    Guid GroupTypeRoleId,
    GroupSyncSourceType SourceType,
    Guid? SourceReferenceId = null,
    string? SourceFilterValue = null,
    int? ScheduleIntervalMinutes = 60,
    bool AddMissingMembers = true,
    bool RemoveExtraMembers = true,
    GroupMemberStatus TargetMemberStatus = GroupMemberStatus.Active
) : ICommand<GroupSyncRuleResponse>;
