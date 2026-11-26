using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Workflow.Contracts.Instances;
using Ruth.Modules.Workflow.Contracts.Triggers;
using Ruth.Modules.Workflow.Domain.Triggers;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Triggers;

internal sealed class DispatchWorkflowAutomationEventCommandHandler(
    WorkflowDbContext dbContext,
    ICommandDispatcher commandDispatcher
) : ICommandHandler<DispatchWorkflowAutomationEventCommand, WorkflowAutomationDispatchResponse>
{
    public async Task<Result<WorkflowAutomationDispatchResponse>> HandleAsync(
        DispatchWorkflowAutomationEventCommand command,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(command.EventTypeKey))
        {
            return Result.Success(new WorkflowAutomationDispatchResponse(0, []));
        }

        var normalizedKey = command.EventTypeKey.Trim().ToLowerInvariant();
        var candidates = await dbContext
            .WorkflowTriggers.AsNoTracking()
            .Where(t =>
                t.IsActive
                && t.TriggerKind == command.TriggerKind
                && t.EventTypeKey == normalizedKey
            )
            .OrderBy(static t => t.Order)
            .ToListAsync(cancellationToken);

        var launchedIds = new List<Guid>();
        foreach (var trigger in candidates)
        {
            if (
                !WorkflowTriggerEvaluator.Matches(
                    trigger,
                    command.TriggerKind,
                    normalizedKey,
                    command.EntityTypeQualifier,
                    command.PayloadAttributes
                )
            )
            {
                continue;
            }

            var wfName = ResolveTriggeredName(trigger.WorkflowNameTemplate, command);
            var launchRes = await commandDispatcher.SendAsync<
                LaunchWorkflowCommand,
                WorkflowInstanceResponse
            >(
                new LaunchWorkflowCommand(
                    trigger.WorkflowTypeId.Value.ToString("D", CultureInfo.InvariantCulture),
                    Name: wfName,
                    InitiatorPersonId: command.InitiatorPersonId,
                    PrimaryEntityTypeKey: command.PrimaryEntityTypeKey,
                    PrimaryEntityId: command.PrimaryEntityId,
                    InitialAttributes: command.PayloadAttributes,
                    ProcessImmediately: true
                ),
                cancellationToken
            );

            if (launchRes.IsSuccess)
            {
                launchedIds.Add(launchRes.Value.Id);
            }
        }

        return Result.Success(
            new WorkflowAutomationDispatchResponse(candidates.Count, launchedIds)
        );
    }

    private static string? ResolveTriggeredName(
        string? template,
        DispatchWorkflowAutomationEventCommand command
    )
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var output = template;
        if (command.PayloadAttributes is not null)
        {
            foreach (var (k, v) in command.PayloadAttributes)
            {
                output = output.Replace(
                    $"{{{{ {k} }}}}",
                    v ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        }

        return output;
    }
}
