using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Workflow.Slices.Events;

/// <summary>Idempotently repoints PersonId references across WorkflowTypes, Workflows, Activities, and Logs upon PersonMergedIntegrationEvent.</summary>
internal sealed class WorkflowPersonMergedConsumer(WorkflowDbContext dbContext)
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

        var types = await dbContext
            .WorkflowTypes.Include(static t => t.Activities)
            .Where(t => t.Activities.Any(a => a.DefaultAssignedPersonId == retired))
            .ToListAsync(cancellationToken);
        foreach (var wfType in types)
        {
            changed |= wfType.RepointPerson(retired, surviving);
        }

        var workflows = await dbContext
            .Workflows.AsSplitQuery()
            .Include(static w => w.Activities)
            .Include(static w => w.Logs)
            .Where(w =>
                w.InitiatorPersonId == retired
                || w.Activities.Any(a => a.AssignedPersonId == retired)
                || w.Logs.Any(l => l.ActorPersonId == retired)
            )
            .ToListAsync(cancellationToken);
        foreach (var instance in workflows)
        {
            changed |= instance.RepointPerson(retired, surviving);
        }

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
