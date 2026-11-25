using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.Modules.Groups.Domain.Attendances;
using Ruth.Modules.Groups.Domain.Groups;
using Ruth.Modules.Groups.Domain.Scheduling;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Scheduling;

internal sealed class GetVolunteerScheduleToolboxQueryHandler(
    GroupsDbContext dbContext,
    IClock clock
) : IQueryHandler<GetVolunteerScheduleToolboxQuery, VolunteerScheduleToolboxResponse>
{
    public async Task<Result<VolunteerScheduleToolboxResponse>> HandleAsync(
        GetVolunteerScheduleToolboxQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var fromDate = query.FromDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var rows = await LoadUpcomingRowsAsync(query.PersonId, fromDate, cancellationToken);
        var exclusions = await dbContext
            .PersonScheduleExclusions.AsNoTracking()
            .Where(e => e.PersonId == query.PersonId && e.EndDate >= fromDate)
            .OrderBy(static e => e.StartDate)
            .ToListAsync(cancellationToken);

        var groups = await LoadGroupsMapAsync(rows, cancellationToken);
        var upcoming = ProjectUpcoming(rows, groups, exclusions);

        return Result.Success(
            new VolunteerScheduleToolboxResponse(
                query.PersonId,
                upcoming,
                [.. exclusions.Select(AddPersonScheduleExclusionCommandHandler.ToResponse)]
            )
        );
    }

    private async Task<List<ScheduledRow>> LoadUpcomingRowsAsync(
        Guid personId,
        DateOnly fromDate,
        CancellationToken cancellationToken
    )
    {
        var rows = await (
            from attendance in dbContext.Attendances.AsNoTracking()
            join occurrence in dbContext.AttendanceOccurrences.AsNoTracking()
                on attendance.OccurrenceId equals occurrence.Id
            where
                attendance.PersonId == personId
                && attendance.RequestedToAttend == true
                && occurrence.OccurrenceDate >= fromDate
            orderby occurrence.OccurrenceDate
            select new { Attendance = attendance, Occurrence = occurrence }
        ).ToListAsync(cancellationToken);

        return [.. rows.Select(static x => new ScheduledRow(x.Attendance, x.Occurrence))];
    }

    private async Task<Dictionary<GroupId, Group>> LoadGroupsMapAsync(
        List<ScheduledRow> rows,
        CancellationToken cancellationToken
    )
    {
        GroupId[] groupIds =
        [
            .. rows.Where(static x => x.Occurrence.GroupId.HasValue)
                .Select(static x => x.Occurrence.GroupId!.Value)
                .Distinct(),
        ];
        return await dbContext
            .Groups.AsNoTracking()
            .Include(static g => g.Locations)
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(static g => g.Id, cancellationToken);
    }

    private static List<ScheduledVolunteerResponse> ProjectUpcoming(
        List<ScheduledRow> rows,
        Dictionary<GroupId, Group> groups,
        List<PersonScheduleExclusion> exclusions
    )
    {
        var upcoming = new List<ScheduledVolunteerResponse>(rows.Count);
        foreach (var item in rows)
        {
            Group? group = null;
            if (item.Occurrence.GroupId.HasValue)
            {
                _ = groups.TryGetValue(item.Occurrence.GroupId.Value, out group);
            }

            if (group is { DisableScheduleToolboxAccess: true })
            {
                continue;
            }

            var locName = group
                ?.Locations.FirstOrDefault(l => l.LocationId == item.Occurrence.LocationId)
                ?.LocationName;
            var hasBlackout =
                item.Occurrence.GroupId.HasValue
                && exclusions.Exists(e =>
                    e.Covers(item.Occurrence.OccurrenceDate, item.Occurrence.GroupId.Value)
                );

            upcoming.Add(
                ScheduleVolunteerService.ToScheduledResponse(
                    item.Attendance,
                    item.Occurrence,
                    group?.Name ?? "Group",
                    locName,
                    hasBlackout,
                    hasDoubleBooking: false,
                    meetsRequirements: true
                )
            );
        }

        return upcoming;
    }

    private sealed record ScheduledRow(Attendance Attendance, AttendanceOccurrence Occurrence);
}
