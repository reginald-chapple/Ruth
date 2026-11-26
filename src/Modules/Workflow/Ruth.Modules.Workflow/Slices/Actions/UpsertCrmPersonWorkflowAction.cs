using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class UpsertCrmPersonWorkflowAction(ICommandDispatcher commandDispatcher)
    : IWorkflowAction
{
    public const string Key = "workflow.crm.ensure-person";

    public string ActionKey => Key;
    public string DisplayName => "Ensure CRM Constituent & Attributes";
    public string Category => "CRM";
    public string Description =>
        "Creates or updates a CRM Person from workflow attributes (FirstName, LastName, Email, PersonId) and syncs optional PersonAttributeKey.";

    public async Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        var personIdAttr = GetConfigOrDefault(cfg, "PersonIdAttributeKey", "PersonId");
        var personId = ResolvePersonId(context, personIdAttr);

        if (!personId.HasValue)
        {
            var createRes = await CreatePersonFromAttributesAsync(context, cfg, cancellationToken);
            if (createRes.IsFailure)
            {
                return Result.Failure<WorkflowActionDirective>(createRes.PrimaryError);
            }

            personId = createRes.Value;
        }

        if (
            cfg.TryGetValue("PersonAttributeKey", out var crmAttrKey)
            && !string.IsNullOrWhiteSpace(crmAttrKey)
        )
        {
            var crmVal = ResolveAttributeValue(context, cfg);
            var attrRes = await commandDispatcher.SendAsync<
                UpdatePersonAttributesCommand,
                PersonDetailResponse
            >(
                new UpdatePersonAttributesCommand(
                    personId.Value,
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        [crmAttrKey.Trim()] = crmVal,
                    }
                ),
                cancellationToken
            );
            if (attrRes.IsFailure)
            {
                return Result.Failure<WorkflowActionDirective>(attrRes.PrimaryError);
            }
        }

        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [personIdAttr] = personId.Value.ToString("D"),
        };

        return Result.Success(
            new WorkflowActionDirective(
                WorkflowActionOutcomeKind.Continue,
                $"Synchronized CRM person {personId.Value:D}.",
                UpdatedWorkflowAttributes: updates
            )
        );
    }

    private async Task<Result<Guid>> CreatePersonFromAttributesAsync(
        WorkflowActionExecutionContext context,
        IReadOnlyDictionary<string, string?> cfg,
        CancellationToken ct
    )
    {
        var firstKey = GetConfigOrDefault(cfg, "FirstNameAttributeKey", "FirstName");
        var lastKey = GetConfigOrDefault(cfg, "LastNameAttributeKey", "LastName");
        var emailKey = GetConfigOrDefault(cfg, "EmailAttributeKey", "Email");

        _ = context.WorkflowAttributes.TryGetValue(firstKey, out var first);
        _ = context.WorkflowAttributes.TryGetValue(lastKey, out var last);
        _ = context.WorkflowAttributes.TryGetValue(emailKey, out var email);

        var res = await commandDispatcher.SendAsync<CreatePersonCommand, PersonDetailResponse>(
            new CreatePersonCommand(
                FirstName: string.IsNullOrWhiteSpace(first) ? "Constituent" : first.Trim(),
                LastName: string.IsNullOrWhiteSpace(last) ? context.WorkflowNumber : last.Trim(),
                Email: string.IsNullOrWhiteSpace(email) ? null : email.Trim()
            ),
            ct
        );

        return res.IsFailure
            ? Result.Failure<Guid>(res.PrimaryError)
            : Result.Success(res.Value.Id);
    }

    private static Guid? ResolvePersonId(WorkflowActionExecutionContext context, string attrKey)
    {
        return
            context.WorkflowAttributes.TryGetValue(attrKey, out var raw)
            && Guid.TryParse(raw, out var parsed)
            && parsed != Guid.Empty
            ? parsed
            : context.InitiatorPersonId;
    }

    private static string? ResolveAttributeValue(
        WorkflowActionExecutionContext context,
        IReadOnlyDictionary<string, string?> cfg
    )
    {
        if (
            cfg.TryGetValue("SourceAttributeKey", out var src)
            && !string.IsNullOrWhiteSpace(src)
            && context.WorkflowAttributes.TryGetValue(src.Trim(), out var v)
        )
        {
            return v;
        }

        _ = cfg.TryGetValue("PersonAttributeValue", out var literal);
        return literal;
    }

    private static string GetConfigOrDefault(
        IReadOnlyDictionary<string, string?> cfg,
        string key,
        string fallback
    )
    {
        return cfg.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.Trim()
            : fallback;
    }
}
