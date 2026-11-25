using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Groups.Domain.Members;

/// <summary>Membership of a CRM Person in a Group under a specific GroupTypeRole.</summary>
public sealed class GroupMember
    : AggregateRoot<GroupMemberId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasAttributes,
        ISecured
{
    /// <summary>Canonical entity type key for GroupMember.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.GroupMember";

    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private GroupMember() { }

    private GroupMember(
        GroupMemberId id,
        GroupId groupId,
        GroupTypeId groupTypeId,
        Guid personId,
        GroupTypeRoleId groupRoleId
    )
        : base(id)
    {
        GroupId = groupId;
        GroupTypeId = groupTypeId;
        PersonId = personId;
        GroupRoleId = groupRoleId;
    }

    /// <summary>Gets the owning Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the denormalized GroupType identifier.</summary>
    public GroupTypeId GroupTypeId { get; private set; }

    /// <summary>Gets the CRM Person identifier.</summary>
    public Guid PersonId { get; private set; }

    /// <summary>Gets the GroupTypeRole identifier.</summary>
    public GroupTypeRoleId GroupRoleId { get; private set; }

    /// <summary>Gets the membership lifecycle status.</summary>
    public GroupMemberStatus Status { get; private set; } = GroupMemberStatus.Active;

    /// <summary>Gets optional leader or admin note.</summary>
    public string? Note { get; private set; }

    /// <summary>Gets optional guest count when the group allows guests.</summary>
    public int? GuestCount { get; private set; }

    /// <summary>Gets when the person was added to the group.</summary>
    public DateTimeOffset? DateTimeAddedUtc { get; private set; }

    /// <summary>Gets when the member became inactive.</summary>
    public DateTimeOffset? InactiveDateTimeUtc { get; private set; }

    /// <summary>Gets whether the member is archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets when the member was archived.</summary>
    public DateTimeOffset? ArchivedDateTimeUtc { get; private set; }

    /// <summary>Gets who archived this member.</summary>
    public Guid? ArchivedByPersonId { get; private set; }

    /// <summary>Gets optional ordering of groups for this person.</summary>
    public int? GroupOrder { get; private set; }

    /// <summary>Gets whether welcome notifications have been sent.</summary>
    public bool IsNotified { get; private set; }

    /// <summary>Gets the optional volunteer schedule cadence template identifier.</summary>
    public GroupMemberScheduleTemplateId? ScheduleTemplateId { get; private set; }

    /// <summary>Gets the optional anchor date for volunteer schedule rotation.</summary>
    public DateOnly? ScheduleStartDate { get; private set; }

    /// <summary>Gets the optional reminder email lead days override.</summary>
    public int? ScheduleReminderEmailOffsetDays { get; private set; }

    /// <summary>Gets the communication channel preference for scheduling.</summary>
    public GroupCommunicationPreference CommunicationPreference { get; private set; }

    /// <summary>Gets the JSONB attributes map persisted by EF Core.</summary>
    public IReadOnlyDictionary<string, string?> Attributes
    {
        get => _attributes;
        private set =>
            _attributes = value is null
                ? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string?>(value, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string?> AttributeValues => _attributes;

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(Group.EntityTypeKeyName, GroupId.Value, GlobalDefaultSecured.Instance);

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

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

    /// <inheritdoc />
    public void SetAttributeValue(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            _attributes[key.Trim()] = value;
        }
    }

    /// <inheritdoc />
    public void RemoveAttributeValue(string key)
    {
        _ = _attributes.Remove(key);
    }

    internal static Result<GroupMember> Create(
        Group group,
        Guid personId,
        GroupTypeRoleId roleId,
        GroupMemberStatus status,
        string? note,
        int? guestCount,
        int? groupOrder,
        GroupMemberScheduleTemplateId? scheduleTemplateId,
        DateOnly? scheduleStartDate,
        int? scheduleReminderEmailOffsetDays,
        GroupCommunicationPreference communicationPreference,
        DateTimeOffset nowUtc
    )
    {
        if (guestCount is < 0)
        {
            return GroupsErrors.InvalidGuestCount;
        }

        var noteResult = GroupsTextPolicy.NormalizeOptional(note, GroupsTextPolicy.NoteMaxLength);
        if (noteResult.IsFailure)
        {
            return Result.Failure<GroupMember>(noteResult.Errors);
        }

        var utcNow = nowUtc.ToUniversalTime();
        var member = new GroupMember(
            GroupMemberId.Create(),
            group.Id,
            group.GroupTypeId,
            personId,
            roleId
        )
        {
            Status = status,
            Note = noteResult.Value,
            GuestCount = group.AllowGuests ? guestCount : null,
            GroupOrder = groupOrder,
            DateTimeAddedUtc = utcNow,
            InactiveDateTimeUtc = status == GroupMemberStatus.Inactive ? utcNow : null,
            ScheduleTemplateId = scheduleTemplateId,
            ScheduleStartDate = scheduleStartDate,
            ScheduleReminderEmailOffsetDays = scheduleReminderEmailOffsetDays,
            CommunicationPreference = communicationPreference,
        };
        return Result.Success(member);
    }

    internal Result Update(
        bool allowGuests,
        GroupTypeRoleId roleId,
        GroupMemberStatus status,
        string? note,
        int? guestCount,
        int? groupOrder,
        GroupMemberScheduleTemplateId? scheduleTemplateId,
        DateOnly? scheduleStartDate,
        int? scheduleReminderEmailOffsetDays,
        GroupCommunicationPreference communicationPreference,
        DateTimeOffset nowUtc
    )
    {
        if (guestCount is < 0)
        {
            return GroupsErrors.InvalidGuestCount;
        }

        var noteResult = GroupsTextPolicy.NormalizeOptional(note, GroupsTextPolicy.NoteMaxLength);
        if (noteResult.IsFailure)
        {
            return noteResult;
        }

        var utcNow = nowUtc.ToUniversalTime();
        var wasInactive = Status == GroupMemberStatus.Inactive;
        GroupRoleId = roleId;
        Status = status;
        Note = noteResult.Value;
        GuestCount = allowGuests ? guestCount : null;
        GroupOrder = groupOrder;
        ScheduleTemplateId = scheduleTemplateId;
        ScheduleStartDate = scheduleStartDate;
        ScheduleReminderEmailOffsetDays = scheduleReminderEmailOffsetDays;
        CommunicationPreference = communicationPreference;

        if (status == GroupMemberStatus.Inactive && !wasInactive)
        {
            InactiveDateTimeUtc = utcNow;
        }
        else if (status != GroupMemberStatus.Inactive)
        {
            InactiveDateTimeUtc = null;
        }

        return Result.Success();
    }

    internal void Archive(Guid? archivedByPersonId, DateTimeOffset nowUtc)
    {
        var utcNow = nowUtc.ToUniversalTime();
        IsArchived = true;
        Status = GroupMemberStatus.Inactive;
        ArchivedDateTimeUtc = utcNow;
        InactiveDateTimeUtc ??= utcNow;
        ArchivedByPersonId = archivedByPersonId;
    }

    internal void RepointPerson(Guid survivingPersonId)
    {
        PersonId = survivingPersonId;
    }

    internal void RepointArchivedByPerson(Guid survivingPersonId)
    {
        ArchivedByPersonId = survivingPersonId;
    }
}
