using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Definitions;

internal sealed class GetWorkflowTypeByIdOrSlugQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetWorkflowTypeByIdOrSlugQuery, WorkflowTypeResponse>
{
    public async Task<Result<WorkflowTypeResponse>> HandleAsync(
        GetWorkflowTypeByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(query.IdOrSlug))
        {
            return Result.Failure<WorkflowTypeResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        var q = dbContext
            .WorkflowTypes.AsNoTracking()
            .AsSplitQuery()
            .Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions);

        var slugCandidate = query.IdOrSlug.Trim().ToLowerInvariant();
        var entity = Guid.TryParse(query.IdOrSlug, out var parsed)
            ? await q.FirstOrDefaultAsync(
                t => t.Id == WorkflowTypeId.From(parsed),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(t => t.Slug == slugCandidate, cancellationToken);

        if (entity is null)
        {
            return Result.Failure<WorkflowTypeResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        var formActionIds = (
            await dbContext
                .WorkflowActionForms.AsNoTracking()
                .Select(static f => f.WorkflowActionTypeId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

        return Result.Success(WorkflowProjectionMapper.MapWorkflowType(entity, formActionIds));
    }
}
