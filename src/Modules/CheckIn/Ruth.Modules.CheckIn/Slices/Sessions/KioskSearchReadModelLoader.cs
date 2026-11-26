using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Groups.Contracts.Scheduling;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class KioskSearchReadModelLoader(
    CheckInDbContext dbContext,
    IQueryDispatcher queryDispatcher
)
{
    public Task<CheckInConfiguration?> LoadConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken
    )
    {
        var id = CheckInConfigurationId.From(configurationId);
        return dbContext
            .CheckInConfigurations.AsNoTracking()
            .Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<Dictionary<Guid, int>> LoadLiveRoomOccupancyAsync(
        CheckInConfigurationId configId,
        DateOnly serviceDate,
        Guid scheduleId,
        CancellationToken cancellationToken
    )
    {
        return dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e =>
                e.CheckInConfigurationId == configId
                && e.ServiceDate == serviceDate
                && e.ScheduleId == scheduleId
                && e.Status == CheckInEntryStatus.CheckedIn
            )
            .GroupBy(static e => e.LocationId)
            .Select(static g => new { LocationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.LocationId, static x => x.Count, cancellationToken);
    }

    public async Task<HashSet<Guid>> LoadAlreadyCheckedInPeopleAsync(
        CheckInConfigurationId configId,
        DateOnly serviceDate,
        Guid scheduleId,
        Guid[] personIds,
        CancellationToken cancellationToken
    )
    {
        var active = await dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e =>
                e.CheckInConfigurationId == configId
                && e.ServiceDate == serviceDate
                && e.ScheduleId == scheduleId
                && e.Status == CheckInEntryStatus.CheckedIn
                && personIds.Contains(e.PersonId)
            )
            .Select(static e => e.PersonId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return [.. active];
    }

    public async Task<Dictionary<Guid, Guid>> LoadRecentAreaSelectionsAsync(
        CheckInConfiguration config,
        DateOnly serviceDate,
        Guid[] personIds,
        CancellationToken cancellationToken
    )
    {
        if (config.AutoSelectDaysBack <= 0)
        {
            return [];
        }

        var cutoffDate = serviceDate.AddDays(-config.AutoSelectDaysBack);
        var recent = await dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e =>
                e.CheckInConfigurationId == config.Id
                && e.ServiceDate >= cutoffDate
                && e.ServiceDate <= serviceDate
                && personIds.Contains(e.PersonId)
            )
            .OrderByDescending(static e => e.CheckedInOnUtc)
            .Select(static e => new { e.PersonId, AreaId = e.CheckInAreaId.Value })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<Guid, Guid>();
        foreach (var row in recent)
        {
            _ = map.TryAdd(row.PersonId, row.AreaId);
        }

        return map;
    }

    public Task<Result<IReadOnlyList<KioskHouseholdLookupResponse>>> LookupHouseholdsAsync(
        string searchTerm,
        (bool AllowPhone, bool AllowName, bool AllowBarcode) flags,
        int maxFamilies,
        CancellationToken cancellationToken
    )
    {
        return queryDispatcher.QueryAsync<
            LookupKioskHouseholdsQuery,
            IReadOnlyList<KioskHouseholdLookupResponse>
        >(
            new LookupKioskHouseholdsQuery(
                searchTerm,
                flags.AllowPhone,
                flags.AllowName,
                flags.AllowBarcode,
                Math.Clamp(maxFamilies, 1, 25),
                ExplicitPersonId: null
            ),
            cancellationToken
        );
    }

    public async Task<(
        IReadOnlyList<Guid> Enrolled,
        IReadOnlyList<Guid> Shifts
    )> ResolveGroupContextAsync(
        Guid personId,
        DateOnly serviceDate,
        CancellationToken cancellationToken
    )
    {
        var memberships = await queryDispatcher.QueryAsync<
            GetPersonGroupMembershipsQuery,
            IReadOnlyList<GroupMemberResponse>
        >(new GetPersonGroupMembershipsQuery(personId, ActiveOnly: true), cancellationToken);

        var enrolledIds = memberships.IsSuccess
            ? memberships.Value.Select(static m => m.GroupId).Distinct().ToArray()
            : [];

        var toolbox = await queryDispatcher.QueryAsync<
            GetVolunteerScheduleToolboxQuery,
            VolunteerScheduleToolboxResponse
        >(new GetVolunteerScheduleToolboxQuery(personId, serviceDate), cancellationToken);

        var shiftIds = toolbox.IsSuccess
            ? toolbox
                .Value.UpcomingAssignments.Where(a =>
                    a.OccurrenceDate == serviceDate && a.Rsvp == RsvpStatus.Yes
                )
                .Select(static a => a.GroupId)
                .Distinct()
                .ToArray()
            : [];

        return (enrolledIds, shiftIds);
    }
}
