using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class GetWorkflowActionFormByActionTypeQueryHandler(WorkflowDbContext dbContext)
    : IQueryHandler<GetWorkflowActionFormByActionTypeQuery, WorkflowActionFormResponse>
{
    public async Task<Result<WorkflowActionFormResponse>> HandleAsync(
        GetWorkflowActionFormByActionTypeQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var actionTypeId = WorkflowActionTypeId.From(query.WorkflowActionTypeId);
        var form = await dbContext
            .WorkflowActionForms.AsNoTracking()
            .AsSplitQuery()
            .Include(static f => f.Sections)
            .Include(static f => f.Fields)
            .FirstOrDefaultAsync(f => f.WorkflowActionTypeId == actionTypeId, cancellationToken);

        return form is null
            ? Result.Failure<WorkflowActionFormResponse>(WorkflowErrors.ActionFormNotFound)
            : Result.Success(WorkflowProjectionMapper.MapActionForm(form));
    }
}
