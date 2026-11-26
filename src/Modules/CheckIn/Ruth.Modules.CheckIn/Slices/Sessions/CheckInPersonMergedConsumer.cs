using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class CheckInPersonMergedConsumer(CheckInDbContext dbContext)
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
        if (retiredId == Guid.Empty || survivingId == Guid.Empty || retiredId == survivingId)
        {
            return;
        }

        var affectedSessions = await dbContext
            .AttendanceCheckInSessions.Include(static s => s.Entries)
            .Where(s =>
                s.CheckedInByPersonId == retiredId
                || s.Entries.Any(e =>
                    e.PersonId == retiredId
                    || e.CheckedOutByPersonId == retiredId
                    || e.OverrideByPersonId == retiredId
                )
            )
            .ToListAsync(cancellationToken);

        var affectedPasses = await dbContext
            .IssuedWalletPasses.Where(p => p.PersonId == retiredId)
            .ToListAsync(cancellationToken);

        if (affectedSessions.Count == 0 && affectedPasses.Count == 0)
        {
            return;
        }

        foreach (var session in affectedSessions)
        {
            session.RepointPerson(retiredId, survivingId);
        }

        foreach (var pass in affectedPasses)
        {
            pass.RepointPerson(retiredId, survivingId);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
