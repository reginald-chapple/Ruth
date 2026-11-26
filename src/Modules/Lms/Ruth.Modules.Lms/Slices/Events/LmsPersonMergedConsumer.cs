using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Lms.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Lms.Slices.Events;

internal sealed class LmsPersonMergedConsumer(LmsDbContext db)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        var retiredId = integrationEvent.RetiredPersonId;
        var survivingId = integrationEvent.SurvivingPersonId;

        var retiredParticipants = await db
            .Participants.Where(p => p.PersonId == retiredId)
            .ToListAsync(cancellationToken);
        var survivingClassIds = (
            await db
                .Participants.AsNoTracking()
                .Where(p => p.PersonId == survivingId)
                .Select(static p => p.LearningClassId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        foreach (var p in retiredParticipants)
        {
            if (survivingClassIds.Contains(p.LearningClassId))
            {
                _ = db.Participants.Remove(p);
            }
            else
            {
                p.RepointPerson(survivingId);
            }
        }

        var completions = await db
            .ActivityCompletions.Where(c =>
                c.PersonId == retiredId || c.GradedByPersonId == retiredId
            )
            .ToListAsync(cancellationToken);
        foreach (var comp in completions)
        {
            comp.RepointPerson(retiredId, survivingId);
        }

        _ = await db.SaveChangesAsync(cancellationToken);
    }
}
