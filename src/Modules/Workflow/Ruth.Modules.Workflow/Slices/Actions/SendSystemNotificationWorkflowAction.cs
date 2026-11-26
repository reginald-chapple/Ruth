using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.Modules.Workflow.Infrastructure;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class SendSystemNotificationWorkflowAction(WorkflowDbContext dbContext)
    : IWorkflowAction
{
    public const string Key = "workflow.comms.send-notification";

    public string ActionKey => Key;
    public string DisplayName => "Send Transactional Notification";
    public string Category => "Communication";
    public string Description =>
        "Stages a SystemNotificationRequestedIntegrationEvent (SystemCommunicationKey, RecipientEmail, RecipientPhoneNumber, RecipientPersonId).";

    public Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        var commKey =
            cfg.TryGetValue("SystemCommunicationKey", out var k) && !string.IsNullOrWhiteSpace(k)
                ? k.Trim()
                : "workflow.notification";

        var email = Resolve(cfg, context.WorkflowAttributes, "RecipientEmail", "Email");
        var name = Resolve(cfg, context.WorkflowAttributes, "RecipientName", "FullName");
        var phone = Resolve(cfg, context.WorkflowAttributes, "RecipientPhoneNumber", "Phone");
        var personId = ResolveGuid(cfg, context.WorkflowAttributes, "RecipientPersonId", "PersonId")
            ?? context.AssignedPersonId
            ?? context.InitiatorPersonId;

        var mergeFields = new Dictionary<string, string?>(
            context.WorkflowAttributes,
            StringComparer.OrdinalIgnoreCase
        )
        {
            ["WorkflowNumber"] = context.WorkflowNumber,
            ["WorkflowName"] = context.WorkflowName,
            ["StateLabel"] = context.StateLabel,
        };

        dbContext.EnqueueIntegrationEvent(
            new SystemNotificationRequestedIntegrationEvent(
                Guid.CreateVersion7(context.NowUtc),
                context.NowUtc,
                commKey,
                RecipientEmail: email,
                RecipientName: name,
                RecipientPhoneNumber: phone,
                RecipientPersonId: personId,
                MergeFields: mergeFields,
                TriggerModule: "Workflow"
            )
        );

        return Task.FromResult(
            Result.Success(
                new WorkflowActionDirective(
                    WorkflowActionOutcomeKind.Continue,
                    $"Queued system notification '{commKey}'."
                )
            )
        );
    }

    private static string? Resolve(
        IReadOnlyDictionary<string, string?> cfg,
        IReadOnlyDictionary<string, string?> attrs,
        string cfgKey,
        string fallbackAttrKey
    )
    {
        return cfg.TryGetValue(cfgKey, out var literal) && !string.IsNullOrWhiteSpace(literal)
                ? literal.Trim()
            : attrs.TryGetValue(fallbackAttrKey, out var val) && !string.IsNullOrWhiteSpace(val)
                ? val.Trim()
            : null;
    }

    private static Guid? ResolveGuid(
        IReadOnlyDictionary<string, string?> cfg,
        IReadOnlyDictionary<string, string?> attrs,
        string cfgKey,
        string fallbackAttrKey
    )
    {
        return cfg.TryGetValue(cfgKey, out var literal) && Guid.TryParse(literal, out var g1) ? g1
            : attrs.TryGetValue(fallbackAttrKey, out var raw) && Guid.TryParse(raw, out var g2) ? g2
            : null;
    }
}
