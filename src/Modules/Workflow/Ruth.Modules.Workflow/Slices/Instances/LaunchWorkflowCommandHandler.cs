using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Domain.Instances;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.Modules.Workflow.Slices.Engine;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Workflow.Slices.Instances;

internal sealed class LaunchWorkflowCommandHandler(
    WorkflowDbContext dbContext,
    WorkflowExecutionEngine engine,
    IClock clock
) : ICommandHandler<LaunchWorkflowCommand, WorkflowInstanceResponse>
{
    public async Task<Result<WorkflowInstanceResponse>> HandleAsync(
        LaunchWorkflowCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.WorkflowTypeIdOrSlug))
        {
            return Result.Failure<WorkflowInstanceResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        var typeQuery = dbContext
            .WorkflowTypes.Include(static t => t.Activities)
                .ThenInclude(static a => a.Actions);

        var slugCandidate = command.WorkflowTypeIdOrSlug.Trim().ToLowerInvariant();
        var wfType = Guid.TryParse(command.WorkflowTypeIdOrSlug, out var parsed)
            ? await typeQuery.FirstOrDefaultAsync(
                t => t.Id == WorkflowTypeId.From(parsed),
                cancellationToken
            )
            : await typeQuery.FirstOrDefaultAsync(t => t.Slug == slugCandidate, cancellationToken);

        if (wfType is null)
        {
            return Result.Failure<WorkflowInstanceResponse>(WorkflowErrors.WorkflowTypeNotFound);
        }

        var launched = WorkflowInstance.Launch(
            wfType,
            command.Name,
            command.InitiatorPersonId,
            command.PrimaryEntityTypeKey,
            command.PrimaryEntityId,
            command.InitialAttributes,
            clock.UtcNow
        );
        if (launched.IsFailure)
        {
            return Result.Failure<WorkflowInstanceResponse>(launched.PrimaryError);
        }

        var instance = launched.Value;
        _ = dbContext.Workflows.Add(instance);

        if (command.ProcessImmediately)
        {
            _ = await engine.ProcessInstanceAsync(instance, wfType, cancellationToken);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(WorkflowProjectionMapper.MapInstance(instance, wfType.Name));
    }
}
