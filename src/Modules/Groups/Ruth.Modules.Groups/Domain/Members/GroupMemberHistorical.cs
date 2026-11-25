using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Groups.Domain.Members;

/// <summary>SCD Type-2 point-in-time historical snapshot of a GroupMember.</summary>
public sealed class GroupMemberHistorical : AggregateRoot<GroupMemberHistoricalId>, IAuditableEntity
{
    private GroupMemberHistorical() { }

    /// <summary>Gets the GroupMember identifier.</summary>
    public GroupMemberId GroupMemberId { get; private set; }

    /// <summary>Gets the Group identifier.</summary>
    public GroupId GroupId { get; private set; }

    /// <summary>Gets the CRM Person identifier.</summary>
    public Guid PersonId { get; private set; }

    /// <summary>Gets the GroupTypeRole identifier at this point in history.</summary>
    public GroupTypeRoleId GroupRoleId { get; private set; }

    /// <summary>Gets the role name at this point in history.</summary>
    public string GroupRoleName { get; private set; } = string.Empty;

    /// <summary>Gets whether the role was a leader role.</summary>
    public bool IsLeader { get; private set; }

    /// <summary>Gets the membership status.</summary>
    public GroupMemberStatus Status { get; private set; }

    /// <summary>Gets whether the membership was archived.</summary>
    public bool IsArchived { get; private set; }

    /// <summary>Gets when the membership was archived.</summary>
    public DateTimeOffset? ArchivedDateTimeUtc { get; private set; }

    /// <summary>Gets who archived the membership.</summary>
    public Guid? ArchivedByPersonId { get; private set; }

    /// <summary>Gets when the membership became inactive.</summary>
    public DateTimeOffset? InactiveDateTimeUtc { get; private set; }

    /// <summary>Gets when this snapshot became effective.</summary>
    public DateTimeOffset EffectiveDateTimeUtc { get; private set; }

    /// <summary>Gets when this snapshot expired.</summary>
    public DateTimeOffset ExpireDateTimeUtc { get; private set; }

    /// <summary>Gets whether this snapshot is current.</summary>
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

    internal static GroupMemberHistorical CreateSnapshot(
        GroupMember member,
        GroupTypeRole role,
        DateTimeOffset effectiveUtc
    )
    {
        return new GroupMemberHistorical
        {
            Id = GroupMemberHistoricalId.Create(),
            GroupMemberId = member.Id,
            GroupId = member.GroupId,
            PersonId = member.PersonId,
            GroupRoleId = role.Id,
            GroupRoleName = role.Name,
            IsLeader = role.IsLeader,
            Status = member.Status,
            IsArchived = member.IsArchived,
            ArchivedDateTimeUtc = member.ArchivedDateTimeUtc,
            ArchivedByPersonId = member.ArchivedByPersonId,
            InactiveDateTimeUtc = member.InactiveDateTimeUtc,
            EffectiveDateTimeUtc = effectiveUtc.ToUniversalTime(),
            ExpireDateTimeUtc = GroupHistorical.MaxExpireDateTimeUtc,
            CurrentRowIndicator = true,
        };
    }

    internal void ExpireAt(DateTimeOffset expireUtc)
    {
        ExpireDateTimeUtc = expireUtc.ToUniversalTime();
        CurrentRowIndicator = false;
    }

    internal void RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (PersonId == retiredPersonId)
        {
            PersonId = survivingPersonId;
        }

        if (ArchivedByPersonId == retiredPersonId)
        {
            ArchivedByPersonId = survivingPersonId;
        }
    }
}
