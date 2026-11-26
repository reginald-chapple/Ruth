using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Definitions;

internal sealed class UpdateWorkflowTypeCommandHandler(
    WorkflowDbContext dbContext,
    IEnumerable<IWorkflowAction> plugins
) : ICommandHandler<UpdateWorkflowTypeCommand, WorkflowTypeResponse>
{
    public async Task<Result<WorkflowTypeResponse>> HandleAsync(
        UpdateWorkflowTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = WorkflowTypeId.From(command.WorkflowTypeId);
        var entity = await dbContext
            .WorkflowTypes.Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null)
        {
            return Result.Failure<WorkflowTypeResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        var registeredKeys = plugins
            .Select(static p => p.ActionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (
            var actionSpec in (command.Spec?.Activities ?? []).SelectMany(static a =>
                a.Actions ?? []
            )
        )
        {
            if (!registeredKeys.Contains(actionSpec.ActionPluginKey))
            {
                return Result.Failure<WorkflowTypeResponse>(WorkflowErrors.ActionPluginNotFound);
            }
        }

        var updateRes = entity.Update(command.Spec!);
        if (updateRes.IsFailure)
        {
            return Result.Failure<WorkflowTypeResponse>(updateRes.PrimaryError);
        }

        var slugConflict = await dbContext.WorkflowTypes.AnyAsync(
            t => t.Id != id && t.Slug == entity.Slug,
            cancellationToken
        );
        if (slugConflict)
        {
            return Result.Failure<WorkflowTypeResponse>(WorkflowErrors.WorkflowTypeSlugConflict);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapWorkflowType(entity));
    }
}
