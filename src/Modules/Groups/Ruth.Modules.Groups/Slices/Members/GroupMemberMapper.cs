using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Domain.GroupTypes;
using Ruth.Modules.Groups.Domain.Members;
using Ruth.Modules.Groups.Infrastructure;

namespace Ruth.Modules.Groups.Slices.Members;

internal static class GroupMemberMapper
{
    public static GroupMemberResponse ToResponse(
        GroupMember member,
        string groupName,
        GroupTypeRole? role,
        string? capacityWarning = null
    )
    {
        return new GroupMemberResponse(
            member.Id.Value,
            member.GroupId.Value,
            groupName,
            member.GroupTypeId.Value,
            member.PersonId,
            member.GroupRoleId.Value,
            role?.Name ?? "Member",
            role?.IsLeader ?? false,
            member.Status,
            member.Note,
            member.GuestCount,
            member.DateTimeAddedUtc,
            member.InactiveDateTimeUtc,
            member.IsArchived,
            member.ArchivedDateTimeUtc,
            member.ArchivedByPersonId,
            member.GroupOrder,
            member.IsNotified,
            member.ScheduleTemplateId?.Value,
            member.ScheduleStartDate,
            member.ScheduleReminderEmailOffsetDays,
            member.CommunicationPreference,
            new Dictionary<string, string?>(
                member.AttributeValues,
                StringComparer.OrdinalIgnoreCase
            ),
            capacityWarning,
            member.CreatedOnUtc,
            member.ModifiedOnUtc
        );
    }

    public static GroupMemberHistoricalResponse ToHistoricalResponse(GroupMemberHistorical row)
    {
        return new GroupMemberHistoricalResponse(
            row.Id.Value,
            row.GroupMemberId.Value,
            row.GroupId.Value,
            row.PersonId,
            row.GroupRoleId.Value,
            row.GroupRoleName,
            row.IsLeader,
            row.Status,
            row.IsArchived,
            row.ArchivedDateTimeUtc,
            row.ArchivedByPersonId,
            row.InactiveDateTimeUtc,
            row.EffectiveDateTimeUtc,
            row.ExpireDateTimeUtc,
            row.CurrentRowIndicator
        );
    }

    public static async Task RotateHistoryIfEnabledAsync(
        GroupsDbContext dbContext,
        GroupType groupType,
        GroupMember member,
        GroupTypeRole role,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (!groupType.EnableGroupHistory)
        {
            return;
        }

        var activeRows = await dbContext
            .GroupMemberHistoricals.Where(h =>
                h.GroupMemberId == member.Id && h.CurrentRowIndicator
            )
            .ToListAsync(cancellationToken);
        foreach (var row in activeRows)
        {
            row.ExpireAt(nowUtc);
        }

        _ = dbContext.GroupMemberHistoricals.Add(
            GroupMemberHistorical.CreateSnapshot(member, role, nowUtc)
        );
    }
}
