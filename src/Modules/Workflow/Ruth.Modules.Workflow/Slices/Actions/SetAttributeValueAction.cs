using System.Globalization;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class SetAttributeValueAction : IWorkflowAction
{
    public const string Key = "workflow.attributes.set-value";

    public string ActionKey => Key;
    public string DisplayName => "Set or Transform Attribute";
    public string Category => "Attributes";
    public string Description =>
        "Sets AttributeKey from Value, SourceAttributeKey, IncrementBy, or UseUtcNow=true.";

    public Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        if (
            !cfg.TryGetValue("AttributeKey", out var targetKey)
            || string.IsNullOrWhiteSpace(targetKey)
        )
        {
            return Task.FromResult(
                Result.Failure<WorkflowActionDirective>(
                    DomainError.Validation(
                        "Workflow.SetAttributeMissingKey",
                        "AttributeKey is required for workflow.attributes.set-value."
                    )
                )
            );
        }

        var trimmedKey = targetKey.Trim();
        var resolvedValue = ResolveValue(context, cfg, trimmedKey);
        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [trimmedKey] = resolvedValue,
        };

        return Task.FromResult(
            Result.Success(
                new WorkflowActionDirective(
                    WorkflowActionOutcomeKind.Continue,
                    $"Set attribute '{trimmedKey}'.",
                    UpdatedWorkflowAttributes: updates
                )
            )
        );
    }

    private static string? ResolveValue(
        WorkflowActionExecutionContext context,
        IReadOnlyDictionary<string, string?> cfg,
        string targetKey
    )
    {
        return
            cfg.TryGetValue("UseUtcNow", out var utcFlag)
            && string.Equals(utcFlag, "true", StringComparison.OrdinalIgnoreCase)
            ? context.NowUtc.ToString("O", CultureInfo.InvariantCulture)
            : TryResolveIncrement(context.WorkflowAttributes, cfg, targetKey)
                ?? TryResolveCopiedOrLiteral(context.WorkflowAttributes, cfg);
    }

    private static string? TryResolveIncrement(
        IReadOnlyDictionary<string, string?> attrs,
        IReadOnlyDictionary<string, string?> cfg,
        string targetKey
    )
    {
        if (
            !cfg.TryGetValue("IncrementBy", out var incRaw)
            || !decimal.TryParse(
                incRaw,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var delta
            )
        )
        {
            return null;
        }

        _ = attrs.TryGetValue(targetKey, out var currentRaw);
        _ = decimal.TryParse(
            currentRaw,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var current
        );
        return (current + delta).ToString(CultureInfo.InvariantCulture);
    }

    private static string? TryResolveCopiedOrLiteral(
        IReadOnlyDictionary<string, string?> attrs,
        IReadOnlyDictionary<string, string?> cfg
    )
    {
        if (
            cfg.TryGetValue("SourceAttributeKey", out var srcKey)
            && !string.IsNullOrWhiteSpace(srcKey)
            && attrs.TryGetValue(srcKey.Trim(), out var copied)
        )
        {
            return copied;
        }

        _ = cfg.TryGetValue("Value", out var literal);
        return literal;
    }
}
