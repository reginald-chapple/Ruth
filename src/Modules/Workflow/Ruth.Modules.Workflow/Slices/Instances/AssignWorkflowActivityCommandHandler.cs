using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class AssignWorkflowActivityCommandHandler(
    WorkflowDbContext dbContext,
    IClock clock
) : ICommandHandler<AssignWorkflowActivityCommand, WorkflowInstanceResponse>
{
    public async Task<Result<WorkflowInstanceResponse>> HandleAsync(
        AssignWorkflowActivityCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var wfId = WorkflowId.From(command.WorkflowId);
        var instance = await dbContext
            .Workflows.AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .Include(static w => w.Logs)
            .FirstOrDefaultAsync(w => w.Id == wfId, cancellationToken);
        if (instance is null)
        {
            return Result.Failure<WorkflowInstanceResponse>(WorkflowErrors.WorkflowNotFound);
        }

        var wfType = await dbContext
            .WorkflowTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == instance.WorkflowTypeId, cancellationToken);

        var res = instance.AssignActivity(
            WorkflowActivityId.From(command.ActivityId),
            command.AssignedPersonId,
            command.AssignedGroupId,
            wfType?.LoggingLevel ?? WorkflowLoggingLevel.Action,
            clock.UtcNow,
            command.ActorPersonId
        );
        if (res.IsFailure)
        {
            return Result.Failure<WorkflowInstanceResponse>(res.PrimaryError);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(
            WorkflowProjectionMapper.MapInstance(instance, wfType?.Name ?? "Workflow")
        );
    }
}
