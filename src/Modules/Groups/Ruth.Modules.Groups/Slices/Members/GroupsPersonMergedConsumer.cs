using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Groups.Infrastructure;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Groups.Slices.Members;

internal sealed class GroupsPersonMergedConsumer(GroupsDbContext dbContext, IClock clock)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;
        var nowUtc = clock.UtcNow;

        await RepointGroupMembersAsync(retiredId, survivingId, nowUtc, cancellationToken);
        await RepointGroupsAndHistoryAsync(retiredId, survivingId, cancellationToken);
        await RepointSchedulingAndAttendanceAsync(retiredId, survivingId, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task RepointGroupMembersAsync(
        Guid retiredId,
        Guid survivingId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var retiredMembers = await dbContext
            .GroupMembers.Where(m => m.PersonId == retiredId || m.ArchivedByPersonId == retiredId)
            .ToListAsync(cancellationToken);

        var survivingKeys = await dbContext
            .GroupMembers.AsNoTracking()
            .Where(m => m.PersonId == survivingId && !m.IsArchived)
            .Select(static m => new { m.GroupId, m.GroupRoleId })
            .ToListAsync(cancellationToken);
        var existingPairs = new HashSet<(Guid, Guid)>(
            survivingKeys.Select(static k => (k.GroupId.Value, k.GroupRoleId.Value))
        );

        foreach (var member in retiredMembers)
        {
            if (member.ArchivedByPersonId == retiredId)
            {
                member.RepointArchivedByPerson(survivingId);
            }

            if (member.PersonId != retiredId)
            {
                continue;
            }

            if (
                !member.IsArchived
                && !existingPairs.Add((member.GroupId.Value, member.GroupRoleId.Value))
            )
            {
                member.Archive(archivedByPersonId: null, nowUtc);
            }
            else
            {
                member.RepointPerson(survivingId);
            }
        }
    }

    private async Task RepointGroupsAndHistoryAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var groups = await dbContext
            .Groups.Include(static g => g.Locations)
            .Where(g =>
                g.ScheduleCoordinatorPersonId == retiredId
                || g.GroupAdministratorPersonId == retiredId
                || g.ArchivedByPersonId == retiredId
                || g.Locations.Any(l => l.GroupMemberPersonId == retiredId)
            )
            .ToListAsync(cancellationToken);
        foreach (var group in groups)
        {
            group.RepointPersonReferences(retiredId, survivingId);
        }

        var groupHistory = await dbContext
            .GroupHistoricals.Where(h => h.ArchivedByPersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var row in groupHistory)
        {
            row.RepointArchivedByPerson(survivingId);
        }

        var memberHistory = await dbContext
            .GroupMemberHistoricals.Where(h =>
                h.PersonId == retiredId || h.ArchivedByPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var row in memberHistory)
        {
            row.RepointPerson(retiredId, survivingId);
        }

        var reqRows = await dbContext
            .GroupMemberRequirements.Where(r =>
                r.ManuallyCompletedByPersonId == retiredId || r.OverriddenByPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var req in reqRows)
        {
            req.RepointActorReferences(retiredId, survivingId);
        }
    }

    private async Task RepointSchedulingAndAttendanceAsync(
        Guid retiredId,
        Guid survivingId,
        CancellationToken cancellationToken
    )
    {
        var exclusions = await dbContext
            .PersonScheduleExclusions.Where(e => e.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        foreach (var exclusion in exclusions)
        {
            exclusion.RepointPerson(survivingId);
        }

        var attendances = await dbContext
            .Attendances.Where(a =>
                a.PersonId == retiredId
                || a.ScheduledByPersonId == retiredId
                || a.CheckedInByPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var attendance in attendances)
        {
            attendance.RepointPerson(retiredId, survivingId);
        }
    }
}
