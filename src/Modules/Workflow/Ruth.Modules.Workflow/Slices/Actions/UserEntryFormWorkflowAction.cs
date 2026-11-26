using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Contracts.Events;
using Ruth.Modules.Workflow.Domain;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class UserEntryFormWorkflowAction(WorkflowDbContext dbContext) : IWorkflowAction
{
    public const string Key = "workflow.forms.user-entry";

    public string ActionKey => Key;
    public string DisplayName => "Interactive User Entry Form";
    public string Category => "Forms";
    public string Description =>
        "Pauses workflow execution in WaitingForInput until the bound W5-S6 WorkflowActionForm is submitted.";

    public async Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var actionTypeId = WorkflowActionTypeId.From(context.ActionTypeId);
        var form = await dbContext
            .WorkflowActionForms.AsNoTracking()
            .FirstOrDefaultAsync(f => f.WorkflowActionTypeId == actionTypeId, cancellationToken);

        var assignPerson = ResolveGuid(
            context.Configuration,
            context.WorkflowAttributes,
            "AssignPersonId"
        );
        var assignGroup = ResolveGuid(
            context.Configuration,
            context.WorkflowAttributes,
            "AssignGroupId"
        );

        StageTaskEvents(
            context,
            assignPerson ?? context.AssignedPersonId,
            assignGroup ?? context.AssignedGroupId,
            form?.NotificationSystemCommunicationKey
        );

        return Result.Success(
            new WorkflowActionDirective(
                WorkflowActionOutcomeKind.WaitForInput,
                "Paused waiting for interactive form submission.",
                AssignPersonId: assignPerson,
                AssignGroupId: assignGroup
            )
        );
    }

    private void StageTaskEvents(
        WorkflowActionExecutionContext context,
        Guid? effectivePerson,
        Guid? effectiveGroup,
        string? commKey
    )
    {
        dbContext.EnqueueIntegrationEvent(
            new WorkflowTaskAssignedIntegrationEvent(
                Guid.CreateVersion7(context.NowUtc),
                context.NowUtc,
                context.WorkflowId,
                context.ActivityId,
                context.WorkflowNumber,
                context.WorkflowName,
                effectivePerson,
                effectiveGroup
            )
        );

        if (!string.IsNullOrWhiteSpace(commKey))
        {
            dbContext.EnqueueIntegrationEvent(
                new SystemNotificationRequestedIntegrationEvent(
                    Guid.CreateVersion7(context.NowUtc),
                    context.NowUtc,
                    commKey,
                    RecipientPersonId: effectivePerson,
                    MergeFields: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["WorkflowNumber"] = context.WorkflowNumber,
                        ["WorkflowName"] = context.WorkflowName,
                    },
                    TriggerModule: "Workflow"
                )
            );
        }
    }

    private static Guid? ResolveGuid(
        IReadOnlyDictionary<string, string?> cfg,
        IReadOnlyDictionary<string, string?> attrs,
        string key
    )
    {
        return cfg.TryGetValue(key, out var raw) && Guid.TryParse(raw, out var g1) ? g1
            : attrs.TryGetValue(key, out var attrVal) && Guid.TryParse(attrVal, out var g2) ? g2
            : null;
    }
}
