using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.VirtualRooms.Slices.Events;

/// <summary>Idempotently repoints and merges PersonId references across VirtualRooms and VirtualRoomSessions upon PersonMergedIntegrationEvent.</summary>
internal sealed class VirtualRoomsPersonMergedConsumer(VirtualRoomsDbContext dbContext)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var retired = integrationEvent.RetiredPersonId;
        var surviving = integrationEvent.SurvivingPersonId;
        if (retired == Guid.Empty || surviving == Guid.Empty || retired == surviving)
        {
            return;
        }

        var changed = false;

        var rooms = await dbContext
            .VirtualRooms.Where(r => r.HostModeratorPersonId == retired)
            .ToListAsync(cancellationToken);
        foreach (var room in rooms)
        {
            changed |= room.RepointPerson(retired, surviving);
        }

        var sessions = await dbContext
            .VirtualRoomSessions.Include(static s => s.Participants)
            .Where(s => s.Participants.Any(p => p.PersonId == retired))
            .ToListAsync(cancellationToken);

        if (sessions.Count > 0)
        {
            var roomThresholds = await dbContext
                .VirtualRooms.AsNoTracking()
                .ToDictionaryAsync(
                    static r => r.Id,
                    static r => r.MinimumAttendanceDwellSeconds,
                    cancellationToken
                );

            foreach (var session in sessions)
            {
                var minDwell = roomThresholds.GetValueOrDefault(session.VirtualRoomId, 180);
                changed |= session.RepointAndMergeParticipants(retired, surviving, minDwell);
            }
        }

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
