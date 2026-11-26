using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Definitions;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Definitions;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Definitions;

internal sealed class CreateWorkflowTypeCommandHandler(
    WorkflowDbContext dbContext,
    IEnumerable<IWorkflowAction> plugins
) : ICommandHandler<CreateWorkflowTypeCommand, WorkflowTypeResponse>
{
    public async Task<Result<WorkflowTypeResponse>> HandleAsync(
        CreateWorkflowTypeCommand command,
        CancellationToken cancellationToken = default
    )
    {
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

        var created = WorkflowType.Create(command.Spec!);
        if (created.IsFailure)
        {
            return Result.Failure<WorkflowTypeResponse>(created.PrimaryError);
        }

        var exists = await dbContext.WorkflowTypes.AnyAsync(
            t => t.Slug == created.Value.Slug,
            cancellationToken
        );
        if (exists)
        {
            return Result.Failure<WorkflowTypeResponse>(WorkflowErrors.WorkflowTypeSlugConflict);
        }

        _ = dbContext.WorkflowTypes.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapWorkflowType(created.Value));
    }
}
