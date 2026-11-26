using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class CreateEngagementRequestWorkflowAction(ICommandDispatcher commandDispatcher)
    : IWorkflowAction
{
    public const string Key = "workflow.engagement.create-request";

    public string ActionKey => Key;
    public string DisplayName => "Create Engagement Pipeline Request";
    public string Category => "Engagement";
    public string Description =>
        "Creates an EngagementRequest in an EngagementOpportunity (OpportunityId, PersonId) and stores EngagementRequestId.";

    public async Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        var opportunityId = ResolveGuid(cfg, context.WorkflowAttributes, "OpportunityId");
        var personId = ResolveGuid(cfg, context.WorkflowAttributes, "PersonId")
            ?? context.InitiatorPersonId;

        if (!opportunityId.HasValue || !personId.HasValue)
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Validation(
                    "Workflow.EngagementActionMissingIds",
                    "OpportunityId and PersonId are required for workflow.engagement.create-request."
                )
            );
        }

        _ = cfg.TryGetValue("Comments", out var comments);
        var res = await commandDispatcher.SendAsync<
            CreateEngagementRequestCommand,
            EngagementRequestResponse
        >(
            new CreateEngagementRequestCommand(
                opportunityId.Value,
                personId.Value,
                Comments: comments ?? $"Created from workflow {context.WorkflowNumber}"
            ),
            cancellationToken
        );

        if (res.IsFailure)
        {
            return Result.Failure<WorkflowActionDirective>(res.PrimaryError);
        }

        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["EngagementRequestId"] = res.Value.Id.ToString("D"),
        };

        return Result.Success(
            new WorkflowActionDirective(
                WorkflowActionOutcomeKind.Continue,
                $"Created engagement request {res.Value.Id:D}.",
                UpdatedWorkflowAttributes: updates
            )
        );
    }

    private static Guid? ResolveGuid(
        IReadOnlyDictionary<string, string?> cfg,
        IReadOnlyDictionary<string, string?> attrs,
        string key
    )
    {
        return cfg.TryGetValue(key, out var raw) && Guid.TryParse(raw, out var g) ? g
            : attrs.TryGetValue(key, out var fromAttr) && Guid.TryParse(fromAttr, out var ga) ? ga
            : null;
    }
}
