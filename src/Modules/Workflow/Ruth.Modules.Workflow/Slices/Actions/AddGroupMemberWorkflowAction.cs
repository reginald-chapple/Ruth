using Ruth.Modules.Groups.Contracts.Members;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class AddGroupMemberWorkflowAction(ICommandDispatcher commandDispatcher)
    : IWorkflowAction
{
    public const string Key = "workflow.groups.add-member";

    public string ActionKey => Key;
    public string DisplayName => "Add Person to Group";
    public string Category => "Groups";
    public string Description =>
        "Adds a CRM Person (PersonIdAttributeKey or InitiatorPersonId) to GroupId (or GroupIdAttributeKey).";

    public async Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        var groupId = ResolveGuid(
            cfg,
            context.WorkflowAttributes,
            "GroupId",
            "GroupIdAttributeKey"
        );
        var personId = ResolveGuid(
            cfg,
            context.WorkflowAttributes,
            "PersonId",
            "PersonIdAttributeKey"
        ) ?? context.InitiatorPersonId;

        if (!groupId.HasValue || !personId.HasValue)
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Validation(
                    "Workflow.GroupActionMissingIds",
                    "Both GroupId and PersonId are required for workflow.groups.add-member."
                )
            );
        }

        var res = await commandDispatcher.SendAsync<AddGroupMemberCommand, GroupMemberResponse>(
            new AddGroupMemberCommand(
                groupId.Value,
                personId.Value,
                SkipRequirementsCheck: true,
                Note: $"Added by workflow {context.WorkflowNumber}"
            ),
            cancellationToken
        );

        return res.IsFailure
            ? Result.Failure<WorkflowActionDirective>(res.PrimaryError)
            : Result.Success(
                new WorkflowActionDirective(
                    WorkflowActionOutcomeKind.Continue,
                    $"Added person {personId.Value:D} to group {groupId.Value:D}."
                )
            );
    }

    private static Guid? ResolveGuid(
        IReadOnlyDictionary<string, string?> cfg,
        IReadOnlyDictionary<string, string?> attrs,
        string literalKey,
        string attrKeyName
    )
    {
        if (cfg.TryGetValue(literalKey, out var direct) && Guid.TryParse(direct, out var g1))
        {
            return g1;
        }

        var lookup =
            cfg.TryGetValue(attrKeyName, out var customKey) && !string.IsNullOrWhiteSpace(customKey)
                ? customKey.Trim()
                : literalKey;
        return attrs.TryGetValue(lookup, out var raw) && Guid.TryParse(raw, out var g2) ? g2 : null;
    }
}
