using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Attributes;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>Hierarchical Group aggregate supporting locations, PostGIS coordinates, demographics, and JSONB attributes.</summary>
public sealed partial class Group
    : AggregateRoot<GroupId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        IHasAttributes,
        ISecured
{
    /// <summary>Canonical entity type key for Group.</summary>
    public const string EntityTypeKeyName = "Ruth.Groups.Group";

    private readonly List<GroupLocation> _locations = [];
    private Dictionary<string, string?> _attributes = new(StringComparer.OrdinalIgnoreCase);

    private Group() { }

    private Group(GroupId id, GroupTypeId groupTypeId, bool isSystem)
        : base(id)
    {
        GroupTypeId = groupTypeId;
        IsSystem = isSystem;
    }

    /// <summary>Gets the GroupType identifier.</summary>
    public GroupTypeId GroupTypeId { get; private set; }

    /// <summary>Gets the optional parent Group identifier.</summary>
    public GroupId? ParentGroupId { get; private set; }

    /// <summary>Gets the optional Core Branch identifier.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the optional Core Schedule identifier.</summary>
    public Guid? ScheduleId { get; private set; }

    /// <summary>Gets the group display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional Core BinaryFile photo identifier.</summary>
    public Guid? PhotoId { get; private set; }

    /// <summary>Gets the optional meeting style (InPerson, Online, Hybrid).</summary>
    public MeetingStyle? MeetingStyle { get; private set; }

    /// <summary>Gets whether the group is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Gets whether the group is visible in public GroupFinder and simple registration.</summary>
    public bool IsPublic { get; private set; } = true;

    /// <summary>Gets whether the group is archived (soft-deleted).</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets whether this is a system-managed group.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets the display sort order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets whether members may specify additional guests.</summary>
    public bool AllowGuests { get; private set; }

    /// <summary>Gets the optional maximum member capacity.</summary>
    public int? GroupCapacity { get; private set; }

    /// <summary>Gets the optional Core DefinedValue group status identifier.</summary>
    public Guid? StatusValueId { get; private set; }

    /// <summary>Gets the optional Core SignatureDocumentTemplate identifier required for members.</summary>
    public Guid? RequiredSignatureDocumentTemplateId { get; private set; }

    /// <summary>Gets when the group became inactive.</summary>
    public DateTimeOffset? InactiveDateTimeUtc { get; private set; }

    /// <summary>Gets when the group was archived.</summary>
    public DateTimeOffset? ArchivedDateTimeUtc { get; private set; }

    /// <summary>Gets the CRM PersonId who archived this group.</summary>
    public Guid? ArchivedByPersonId { get; private set; }

    /// <summary>Gets whether volunteers must meet all requirements before being scheduled.</summary>
    public bool SchedulingMustMeetRequirements { get; private set; }

    /// <summary>Gets the CRM PersonId notified of volunteer schedule changes.</summary>
    public Guid? ScheduleCoordinatorPersonId { get; private set; }

    /// <summary>Gets the CRM PersonId administering this group.</summary>
    public Guid? GroupAdministratorPersonId { get; private set; }

    /// <summary>Gets whether volunteer scheduling is disabled for this group.</summary>
    public bool DisableScheduling { get; private set; }

    /// <summary>Gets whether the volunteer schedule toolbox is disabled for this group.</summary>
    public bool DisableScheduleToolboxAccess { get; private set; }

    /// <summary>Gets the optional lead days for RSVP reminders.</summary>
    public int? RsvpReminderOffsetDays { get; private set; }

    /// <summary>Gets the attached GroupLocations.</summary>
    public IReadOnlyCollection<GroupLocation> Locations => _locations.AsReadOnly();

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
        ParentGroupId.HasValue
            ? new SecuredEntityRef(
                EntityTypeKeyName,
                ParentGroupId.Value.Value,
                new SecuredEntityRef(
                    GroupType.EntityTypeKeyName,
                    GroupTypeId.Value,
                    GlobalDefaultSecured.Instance
                )
            )
            : new SecuredEntityRef(
                GroupType.EntityTypeKeyName,
                GroupTypeId.Value,
                GlobalDefaultSecured.Instance
            );

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
