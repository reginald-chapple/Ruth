using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Definitions;

internal sealed class GetWorkflowTypesQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetWorkflowTypesQuery, IReadOnlyList<WorkflowTypeResponse>>
{
    public async Task<Result<IReadOnlyList<WorkflowTypeResponse>>> HandleAsync(
        GetWorkflowTypesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext
            .WorkflowTypes.AsNoTracking()
            .AsSplitQuery()
            .Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions)
            .AsQueryable();

        if (!query.IncludeInactive)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        var formActionIds = (
            await dbContext
                .WorkflowActionForms.AsNoTracking()
                .Select(static f => f.WorkflowActionTypeId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        IReadOnlyList<WorkflowTypeResponse> result =
        [
            .. items.Select(t => WorkflowProjectionMapper.MapWorkflowType(t, formActionIds)),
        ];
        return Result.Success(result);
    }
}
