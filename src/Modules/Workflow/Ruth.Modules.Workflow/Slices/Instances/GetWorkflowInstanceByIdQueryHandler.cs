using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class GetWorkflowInstanceByIdQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetWorkflowInstanceByIdQuery, WorkflowInstanceResponse>
{
    public async Task<Result<WorkflowInstanceResponse>> HandleAsync(
        GetWorkflowInstanceByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var wfId = WorkflowId.From(query.WorkflowId);
        var entity = await dbContext
            .Workflows.AsNoTracking()
            .AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .Include(static w => w.Logs)
            .FirstOrDefaultAsync(w => w.Id == wfId, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<WorkflowInstanceResponse>(WorkflowErrors.WorkflowNotFound);
        }

        var wfType = await dbContext
            .WorkflowTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == entity.WorkflowTypeId, cancellationToken);

        return Result.Success(
            WorkflowProjectionMapper.MapInstance(entity, wfType?.Name ?? "Workflow")
        );
    }
}
