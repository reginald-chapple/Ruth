using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Sync;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Groups.Domain.Sync;

/// <summary>Configuration binding a Group + GroupTypeRole to an authoritative person source for scheduled GroupSync.</summary>
public sealed class GroupSyncRule
    : AggregateRoot<GroupSyncRuleId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    /// <summary>Canonical entity type key for GroupSyncRule.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.GroupSyncRule";

    private GroupSyncRule() { }

    private GroupSyncRule(GroupSyncRuleId id, GroupId groupId, GroupTypeRoleId roleId)
        : base(id)
    {
        GroupId = groupId;
        GroupTypeRoleId = roleId;
    }

    /// <summary>Gets the target Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the target GroupTypeRole identifier.</summary>
    public GroupTypeRoleId GroupTypeRoleId { get; private set; }

    /// <summary>Gets the external source kind.</summary>
    public GroupSyncSourceType SourceType { get; private set; }

    /// <summary>Gets the optional source reference ID (Core EntitySetId or source GroupId).</summary>
    public Guid? SourceReferenceId { get; private set; }

    /// <summary>Gets the optional source filter value (e.g., CRM PersonConnectionStatus name).</summary>
    public string? SourceFilterValue { get; private set; }

    /// <summary>Gets the minimum interval in minutes between scheduled reconciliations.</summary>
    public int? ScheduleIntervalMinutes { get; private set; }

    /// <summary>Gets whether missing people from the source are added to the group role.</summary>
    public bool AddMissingMembers { get; private set; } = true;

    /// <summary>Gets whether members absent from the source are archived/removed from the group role.</summary>
    public bool RemoveExtraMembers { get; private set; } = true;

    /// <summary>Gets the GroupMemberStatus assigned to newly synced members.</summary>
    public GroupMemberStatus TargetMemberStatus { get; private set; } = GroupMemberStatus.Active;

    /// <summary>Gets when this rule last synchronized.</summary>
    public DateTimeOffset? LastSyncOnUtc { get; private set; }

    /// <summary>Gets the number of members added in the last run.</summary>
    public int LastAddedCount { get; private set; }

    /// <summary>Gets the number of members removed in the last run.</summary>
    public int LastRemovedCount { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <summary>Returns true if this rule is due for execution at <paramref name="nowUtc" />.</summary>
    public bool IsDue(DateTimeOffset nowUtc)
    {
        if (!LastSyncOnUtc.HasValue)
        {
            return true;
        }

        var intervalMinutes = ScheduleIntervalMinutes ?? 60;
        return nowUtc.ToUniversalTime() >= LastSyncOnUtc.Value.AddMinutes(intervalMinutes);
    }

    internal static Result<GroupSyncRule> Create(
        GroupId groupId,
        GroupTypeRoleId roleId,
        GroupSyncSourceType sourceType,
        Guid? sourceReferenceId,
        string? sourceFilterValue,
        int? scheduleIntervalMinutes,
        bool addMissingMembers,
        bool removeExtraMembers,
        GroupMemberStatus targetMemberStatus
    )
    {
        var rule = new GroupSyncRule(GroupSyncRuleId.Create(), groupId, roleId);
        var update = rule.Update(
            sourceType,
            sourceReferenceId,
            sourceFilterValue,
            scheduleIntervalMinutes,
            addMissingMembers,
            removeExtraMembers,
            targetMemberStatus
        );
        return update.IsFailure
            ? Result.Failure<GroupSyncRule>(update.Errors)
            : Result.Success(rule);
    }

    internal Result Update(
        GroupSyncSourceType sourceType,
        Guid? sourceReferenceId,
        string? sourceFilterValue,
        int? scheduleIntervalMinutes,
        bool addMissingMembers,
        bool removeExtraMembers,
        GroupMemberStatus targetMemberStatus
    )
    {
        if (!HasValidSource(sourceType, sourceReferenceId, sourceFilterValue))
        {
            return GroupsErrors.InvalidSyncSource;
        }

        if (scheduleIntervalMinutes is <= 0)
        {
            return GroupsErrors.InvalidCapacity;
        }

        SourceType = sourceType;
        SourceReferenceId = sourceReferenceId;
        SourceFilterValue = sourceFilterValue?.Trim();
        ScheduleIntervalMinutes = scheduleIntervalMinutes;
        AddMissingMembers = addMissingMembers;
        RemoveExtraMembers = removeExtraMembers;
        TargetMemberStatus = targetMemberStatus;
        return Result.Success();
    }

    internal void RecordExecution(DateTimeOffset executedOnUtc, int addedCount, int removedCount)
    {
        LastSyncOnUtc = executedOnUtc.ToUniversalTime();
        LastAddedCount = addedCount;
        LastRemovedCount = removedCount;
    }

    private static bool HasValidSource(
        GroupSyncSourceType sourceType,
        Guid? sourceReferenceId,
        string? sourceFilterValue
    )
    {
        return sourceType switch
        {
            GroupSyncSourceType.EntitySet or GroupSyncSourceType.GroupMembers =>
                sourceReferenceId.HasValue && sourceReferenceId.Value != Guid.Empty,
            GroupSyncSourceType.CrmConnectionStatus => !string.IsNullOrWhiteSpace(
                sourceFilterValue
            ),
            _ => false,
        };
    }
}
