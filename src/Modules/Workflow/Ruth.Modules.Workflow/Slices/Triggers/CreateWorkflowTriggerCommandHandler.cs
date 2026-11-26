using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Triggers;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Triggers;

internal sealed class CreateWorkflowTriggerCommandHandler(WorkflowDbContext dbContext)
    : ICommandHandler<CreateWorkflowTriggerCommand, WorkflowTriggerResponse>
{
    public async Task<Result<WorkflowTriggerResponse>> HandleAsync(
        CreateWorkflowTriggerCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = WorkflowTrigger.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<WorkflowTriggerResponse>(created.PrimaryError);
        }

        var wfType = await dbContext
            .WorkflowTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == created.Value.WorkflowTypeId, cancellationToken);
        if (wfType is null)
        {
            return Result.Failure<WorkflowTriggerResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        _ = dbContext.WorkflowTriggers.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapTrigger(created.Value, wfType.Name));
    }
}
