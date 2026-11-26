using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Forms;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Forms;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Forms;

internal sealed class UpsertWorkflowActionFormCommandHandler(WorkflowDbContext dbContext)
    : ICommandHandler<UpsertWorkflowActionFormCommand, WorkflowActionFormResponse>
{
    public async Task<Result<WorkflowActionFormResponse>> HandleAsync(
        UpsertWorkflowActionFormCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var actionTypeId = WorkflowActionTypeId.From(command.WorkflowActionTypeId);
        var actionExists = await dbContext.WorkflowActionTypes.AnyAsync(
            a => a.Id == actionTypeId,
            cancellationToken
        );
        if (!actionExists)
        {
            return Result.Failure<WorkflowActionFormResponse>(WorkflowErrors.ActionFormNotFound);
        }

        var existing = await dbContext
            .WorkflowActionForms.AsSplitQuery()
            .Include(static f => f.Sections)
            .Include(static f => f.Fields)
            .FirstOrDefaultAsync(f => f.WorkflowActionTypeId == actionTypeId, cancellationToken);

        if (existing is not null)
        {
            var upd = existing.Update(command.Spec);
            if (upd.IsFailure)
            {
                return Result.Failure<WorkflowActionFormResponse>(upd.PrimaryError);
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(WorkflowProjectionMapper.MapActionForm(existing));
        }

        var created = WorkflowActionForm.Create(actionTypeId, command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<WorkflowActionFormResponse>(created.PrimaryError);
        }

        _ = dbContext.WorkflowActionForms.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapActionForm(created.Value));
    }
}
