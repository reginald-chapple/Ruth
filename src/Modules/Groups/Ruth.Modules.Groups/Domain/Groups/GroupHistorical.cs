using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Groups;

/// <summary>SCD Type-2 point-in-time historical snapshot of a Group.</summary>
public sealed class GroupHistorical : AggregateRoot<GroupHistoricalId>, IAuditableEntity
{
    /// <summary>Sentinel expiration timestamp for the currently active historical row.</summary>
    public static readonly DateTimeOffset MaxExpireDateTimeUtc = new(
        9999,
        1,
        1,
        0,
        0,
        0,
        TimeSpan.Zero
    );

    private GroupHistorical() { }

    /// <summary>Gets the Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the Group name at this point in history.</summary>
    public string GroupName { get; private set; } = string.Empty;

    /// <summary>Gets the GroupType identifier.</summary>
    public GroupTypeId GroupTypeId { get; private set; }

    /// <summary>Gets the GroupType name at this point in history.</summary>
    public string GroupTypeName { get; private set; } = string.Empty;

    /// <summary>Gets the optional Branch identifier.</summary>
    public Guid? BranchId { get; private set; }

    /// <summary>Gets the optional parent Group identifier.</summary>
    public GroupId? ParentGroupId { get; private set; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional status value identifier.</summary>
    public Guid? StatusValueId { get; private set; }

    /// <summary>Gets the optional schedule identifier.</summary>
    public Guid? ScheduleId { get; private set; }

    /// <summary>Gets whether the group was active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Gets whether the group was archived.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets when the group became inactive.</summary>
    public DateTimeOffset? InactiveDateTimeUtc { get; private set; }

    /// <summary>Gets when the group was archived.</summary>
    public DateTimeOffset? ArchivedDateTimeUtc { get; private set; }

    /// <summary>Gets who archived the group.</summary>
    public Guid? ArchivedByPersonId { get; private set; }

    /// <summary>Gets when this snapshot became effective.</summary>
    public DateTimeOffset EffectiveDateTimeUtc { get; private set; }

    /// <summary>Gets when this snapshot expired (9999-01-01 when current).</summary>
    public DateTimeOffset ExpireDateTimeUtc { get; private set; }

    /// <summary>Gets whether this snapshot is the current row.</summary>
    public bool CurrentRowIndicator { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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

    internal static GroupHistorical CreateSnapshot(
        Group group,
        string groupTypeName,
        DateTimeOffset effectiveUtc
    )
    {
        return new GroupHistorical
        {
            Id = GroupHistoricalId.Create(),
            GroupId = group.Id,
            GroupName = group.Name,
            GroupTypeId = group.GroupTypeId,
            GroupTypeName = groupTypeName,
            BranchId = group.BranchId,
            ParentGroupId = group.ParentGroupId,
            Description = group.Description,
            StatusValueId = group.StatusValueId,
            ScheduleId = group.ScheduleId,
            IsActive = group.IsActive,
            IsArchived = group.IsArchived,
            InactiveDateTimeUtc = group.InactiveDateTimeUtc,
            ArchivedDateTimeUtc = group.ArchivedDateTimeUtc,
            ArchivedByPersonId = group.ArchivedByPersonId,
            EffectiveDateTimeUtc = effectiveUtc.ToUniversalTime(),
            ExpireDateTimeUtc = MaxExpireDateTimeUtc,
            CurrentRowIndicator = true,
        };
    }

    internal void ExpireAt(DateTimeOffset expireUtc)
    {
        ExpireDateTimeUtc = expireUtc.ToUniversalTime();
        CurrentRowIndicator = false;
    }

    internal void RepointArchivedByPerson(Guid survivingPersonId)
    {
        ArchivedByPersonId = survivingPersonId;
    }
}
