using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Triggers;

internal sealed class UpdateWorkflowTriggerCommandHandler(WorkflowDbContext dbContext)
    : ICommandHandler<UpdateWorkflowTriggerCommand, WorkflowTriggerResponse>
{
    public async Task<Result<WorkflowTriggerResponse>> HandleAsync(
        UpdateWorkflowTriggerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = WorkflowTriggerId.From(command.TriggerId);
        var entity = await dbContext.WorkflowTriggers.FirstOrDefaultAsync(
            t => t.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return Result.Failure<WorkflowTriggerResponse>(WorkflowErrors.TriggerNotFound);
        }

        var res = entity.Update(command.Spec);
        if (res.IsFailure)
        {
            return Result.Failure<WorkflowTriggerResponse>(res.PrimaryError);
        }

        var wfType = await dbContext
            .WorkflowTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == entity.WorkflowTypeId, cancellationToken);
        if (wfType is null)
        {
            return Result.Failure<WorkflowTriggerResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapTrigger(entity, wfType.Name));
    }
}
