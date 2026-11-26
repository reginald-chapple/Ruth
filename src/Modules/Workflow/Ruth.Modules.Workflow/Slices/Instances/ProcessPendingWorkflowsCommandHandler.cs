using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Engine;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class ProcessPendingWorkflowsCommandHandler(
    WorkflowDbContext dbContext,
    WorkflowExecutionEngine engine,
    IClock clock
) : ICommandHandler<ProcessPendingWorkflowsCommand, WorkflowBatchProcessResponse>
{
    public async Task<Result<WorkflowBatchProcessResponse>> HandleAsync(
        ProcessPendingWorkflowsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var dueInstances = await LoadDueInstancesAsync(command.MaxBatchSize, cancellationToken);
        if (dueInstances.Count == 0)
        {
            return Result.Success(new WorkflowBatchProcessResponse(0, 0, 0, 0));
        }

        List<WorkflowTypeId> typeIds =
        [
            .. dueInstances.Select(static w => w.WorkflowTypeId).Distinct(),
        ];
        var types = await dbContext
            .WorkflowTypes.AsNoTracking()
            .AsSplitQuery()
            .Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions)
            .Where(t => typeIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        var summary = await ExecuteBatchAsync(dueInstances, types, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(summary);
    }

    private async Task<List<WorkflowInstance>> LoadDueInstancesAsync(
        int maxBatchSize,
        CancellationToken ct
    )
    {
        var now = clock.UtcNow;
        var limit = Math.Clamp(maxBatchSize, 1, 250);
        return await dbContext
            .Workflows.AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .Include(static w => w.Logs)
            .Where(w =>
                (
                    w.Status == WorkflowInstanceStatus.Pending
                    || w.Status == WorkflowInstanceStatus.Active
                    || w.Status == WorkflowInstanceStatus.Suspended
                ) && (w.NextScheduledProcessAtUtc == null || w.NextScheduledProcessAtUtc <= now)
            )
            .OrderBy(static w => w.NextScheduledProcessAtUtc)
            .Take(limit)
            .ToListAsync(ct);
    }

    private async Task<WorkflowBatchProcessResponse> ExecuteBatchAsync(
        List<WorkflowInstance> dueInstances,
        Dictionary<WorkflowTypeId, WorkflowType> types,
        CancellationToken ct
    )
    {
        int completed = 0,
            waiting = 0,
            faulted = 0;

        foreach (var instance in dueInstances)
        {
            if (!types.TryGetValue(instance.WorkflowTypeId, out var wfType))
            {
                continue;
            }

            _ = await engine.ProcessInstanceAsync(instance, wfType, ct);
            if (instance.Status == WorkflowInstanceStatus.Completed)
            {
                completed++;
            }
            else if (
                instance.Status
                is WorkflowInstanceStatus.WaitingForInput
                    or WorkflowInstanceStatus.Suspended
            )
            {
                waiting++;
            }
            else if (instance.Status == WorkflowInstanceStatus.Faulted)
            {
                faulted++;
            }
        }

        return new WorkflowBatchProcessResponse(dueInstances.Count, completed, waiting, faulted);
    }
}
