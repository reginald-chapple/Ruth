using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Engine;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class ProcessWorkflowCommandHandler(
    WorkflowDbContext dbContext,
    WorkflowExecutionEngine engine
) : ICommandHandler<ProcessWorkflowCommand, WorkflowInstanceResponse>
{
    public async Task<Result<WorkflowInstanceResponse>> HandleAsync(
        ProcessWorkflowCommand command,
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
            .AsSplitQuery()
            .Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions)
            .FirstOrDefaultAsync(t => t.Id == instance.WorkflowTypeId, cancellationToken);
        if (wfType is null)
        {
            return Result.Failure<WorkflowInstanceResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        _ = await engine.ProcessInstanceAsync(instance, wfType, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapInstance(instance, wfType.Name));
    }
}
