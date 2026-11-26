using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class GetWorkflowInstancesQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetWorkflowInstancesQuery, IReadOnlyList<WorkflowInstanceResponse>>
{
    public async Task<Result<IReadOnlyList<WorkflowInstanceResponse>>> HandleAsync(
        GetWorkflowInstancesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext
            .Workflows.AsNoTracking()
            .AsSplitQuery()
            .Include(static w => w.Activities)
                .ThenInclude(static a => a.Actions)
            .Include(static w => w.Logs)
            .AsQueryable();

        if (query.WorkflowTypeId.HasValue)
        {
            var typeId = WorkflowTypeId.From(query.WorkflowTypeId.Value);
            q = q.Where(w => w.WorkflowTypeId == typeId);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(w => w.Status == query.Status.Value);
        }

        if (query.InitiatorPersonId.HasValue)
        {
            q = q.Where(w => w.InitiatorPersonId == query.InitiatorPersonId.Value);
        }

        var limit = Math.Clamp(query.Limit, 1, 200);
        var items = await q.OrderByDescending(static w => w.ActivatedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var typeNames = await dbContext
            .WorkflowTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        IReadOnlyList<WorkflowInstanceResponse> result =
        [
            .. items.Select(w =>
                WorkflowProjectionMapper.MapInstance(
                    w,
                    typeNames.GetValueOrDefault(w.WorkflowTypeId, "Workflow")
                )
            ),
        ];
        return Result.Success(result);
    }
}
