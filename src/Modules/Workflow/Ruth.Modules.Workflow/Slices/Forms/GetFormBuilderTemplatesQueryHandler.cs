using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class GetFormBuilderTemplatesQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetFormBuilderTemplatesQuery, IReadOnlyList<FormBuilderTemplateResponse>>
{
    public async Task<Result<IReadOnlyList<FormBuilderTemplateResponse>>> HandleAsync(
        GetFormBuilderTemplatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var q = dbContext.FormBuilderTemplates.AsNoTracking().AsQueryable();
        if (!query.IncludeInactive)
        {
            q = q.Where(static t => t.IsActive);
        }

        var items = await q.OrderBy(static t => t.Name).ToListAsync(cancellationToken);
        IReadOnlyList<FormBuilderTemplateResponse> result =
        [
            .. items.Select(WorkflowProjectionMapper.MapFormTemplate),
        ];
        return Result.Success(result);
    }
}
