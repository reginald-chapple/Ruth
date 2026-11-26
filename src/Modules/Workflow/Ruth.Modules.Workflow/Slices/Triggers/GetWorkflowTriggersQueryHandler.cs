using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Triggers;

internal sealed class GetWorkflowTriggersQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetWorkflowTriggersQuery, IReadOnlyList<WorkflowTriggerResponse>>
{
    public async Task<Result<IReadOnlyList<WorkflowTriggerResponse>>> HandleAsync(
        GetWorkflowTriggersQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext.WorkflowTriggers.AsNoTracking().AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Order)
            .ThenBy(static t => t.Name)
            .ToListAsync(cancellationToken);
        var typeNames = await dbContext
            .WorkflowTypes.AsNoTracking()
            .ToDictionaryAsync(static t => t.Id, static t => t.Name, cancellationToken);

        IReadOnlyList<WorkflowTriggerResponse> result =
        [
            .. items.Select(t =>
                WorkflowProjectionMapper.MapTrigger(
                    t,
                    typeNames.GetValueOrDefault(t.WorkflowTypeId, "Workflow")
                )
            ),
        ];
        return Result.Success(result);
    }
}
