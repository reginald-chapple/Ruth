using Ruth.Modules.Groups.Contracts.GroupTypes;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Groups.Domain.GroupTypes;

/// <summary>Aggregate defining structural rules, roles, attendance behavior, and allowed hierarchy for a family of Groups.</summary>
public sealed partial class GroupType
    : AggregateRoot<GroupTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasAttributes,
        ISecured
{
    /// <summary>Canonical entity type key for GroupType.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.GroupType";

    private readonly List<GroupTypeRole> _roles = [];
    private readonly List<GroupTypeAllowedChild> _allowedChildren = [];
    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private GroupType() { }

    private GroupType(GroupTypeId id, bool isSystem)
        : base(id)
    {
        IsSystem = isSystem;
    }

    /// <summary>Gets the GroupType display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the singular display term for groups of this type.</summary>
    public string GroupTerm { get; private set; } = "Group";

    /// <summary>Gets the singular display term for members of groups of this type.</summary>
    public string GroupMemberTerm { get; private set; } = "Member";

    /// <summary>Gets the optional icon CSS class or Fluent icon name.</summary>
    public string? IconCssClass { get; private set; }

    /// <summary>Gets the sort order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether this is a system-managed GroupType.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets whether groups of this type appear in group lists.</summary>
    public bool ShowInGroupList { get; private set; } = true;

    /// <summary>Gets whether groups of this type appear in navigation trees.</summary>
    public bool ShowInNavigation { get; private set; } = true;

    /// <summary>Gets whether groups of this type may attach more than one GroupLocation.</summary>
    public bool AllowMultipleLocations { get; private set; }

    /// <summary>Gets whether groups of this type expose a MeetingStyle.</summary>
    public bool IsMeetingStyleEnabled { get; private set; }

    /// <summary>Gets whether groups of this type must have a CampusId.</summary>
    public bool GroupsRequireCampus { get; private set; }

    /// <summary>Gets whether SCD Type-2 GroupHistorical and GroupMemberHistorical snapshots are maintained.</summary>
    public bool EnableGroupHistory { get; private set; }

    /// <summary>Gets whether groups of this type support taking attendance.</summary>
    public bool TakesAttendance { get; private set; }

    /// <summary>Gets whether attendance counts toward weekend service totals.</summary>
    public bool AttendanceCountsAsWeekendService { get; private set; }

    /// <summary>Gets whether attendance reminders are enabled for leaders.</summary>
    public bool SendAttendanceReminder { get; private set; }

    /// <summary>Gets whether recording attendance requires a LocationId.</summary>
    public bool GroupAttendanceRequiresLocation { get; private set; }

    /// <summary>Gets whether recording attendance requires a ScheduleId.</summary>
    public bool GroupAttendanceRequiresSchedule { get; private set; }

    /// <summary>Gets the enrollment rule for attendance and check-in.</summary>
    public AttendanceRule AttendanceRule { get; private set; }

    /// <summary>Gets the capacity enforcement rule for groups of this type.</summary>
    public GroupCapacityRule GroupCapacityRule { get; private set; }

    /// <summary>Gets the permitted location selection modes.</summary>
    public GroupLocationPickerMode LocationSelectionMode { get; private set; } =
        GroupLocationPickerMode.Named;

    /// <summary>Gets whether location-level schedules are enabled.</summary>
    public bool EnableLocationSchedules { get; private set; }

    /// <summary>Gets whether volunteer serving scheduling is enabled.</summary>
    public bool IsSchedulingEnabled { get; private set; }

    /// <summary>Gets whether scheduled volunteers must RSVP or auto-confirm.</summary>
    public ScheduleConfirmationLogic ScheduleConfirmationLogic { get; private set; }

    /// <summary>Gets the optional reminder lead time in days.</summary>
    public int? ScheduleReminderEmailOffsetDays { get; private set; }

    /// <summary>Gets the optional Core DefinedValue purpose identifier.</summary>
    public Guid? PurposeValueId { get; private set; }

    /// <summary>Gets the default role assigned to new members.</summary>
    public GroupTypeRoleId? DefaultGroupRoleId { get; private set; }

    /// <summary>Gets the optional inherited GroupType identifier.</summary>
    public GroupTypeId? InheritedGroupTypeId { get; private set; }

    /// <summary>Gets the roles defined for this GroupType.</summary>
    public IReadOnlyCollection<GroupTypeRole> Roles => _roles.AsReadOnly();

    /// <summary>Gets the allowed child GroupType relationships.</summary>
    public IReadOnlyCollection<GroupTypeAllowedChild> AllowedChildren =>
        _allowedChildren.AsReadOnly();

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
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

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
}
