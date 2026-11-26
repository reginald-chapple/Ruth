using System.Globalization;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class ConditionalActivateActivityAction : IWorkflowAction
{
    public const string Key = "workflow.control.activate-activity";

    public string ActionKey => Key;
    public string DisplayName => "Conditional Activate Activity";
    public string Category => "Control Flow";
    public string Description =>
        "Evaluates an optional attribute condition (SourceAttributeKey, Operator, ExpectedValue) and activates TargetActivityKey or FallbackActivityKey.";

    public Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        var cfg = context.Configuration;
        _ = cfg.TryGetValue("TargetActivityKey", out var targetKey);
        _ = cfg.TryGetValue("FallbackActivityKey", out var fallbackKey);
        _ = cfg.TryGetValue("SourceAttributeKey", out var attrKey);
        _ = cfg.TryGetValue("Operator", out var opRaw);
        _ = cfg.TryGetValue("ExpectedValue", out var expected);

        var matched =
            string.IsNullOrWhiteSpace(attrKey)
            || Evaluate(context.WorkflowAttributes, attrKey, opRaw, expected);

        var chosen = matched ? targetKey : fallbackKey;
        return Task.FromResult(Result.Success(BuildOutcomeDirective(chosen)));
    }

    private static WorkflowActionDirective BuildOutcomeDirective(string? chosen)
    {
        return string.IsNullOrWhiteSpace(chosen)
            ? new WorkflowActionDirective(
                WorkflowActionOutcomeKind.Continue,
                "Condition did not select a target activity; continuing."
            )
            : new WorkflowActionDirective(
                WorkflowActionOutcomeKind.ActivateActivity,
                $"Routing to activity '{chosen.Trim()}'.",
                TargetActivityKey: chosen.Trim()
            );
    }

    private static bool Evaluate(
        IReadOnlyDictionary<string, string?> attrs,
        string attrKey,
        string? opRaw,
        string? expected
    )
    {
        _ = attrs.TryGetValue(attrKey.Trim(), out var actual);
        var mode = string.IsNullOrWhiteSpace(opRaw) ? "equals" : opRaw.Trim().ToLowerInvariant();
        return mode switch
        {
            "notequals" => !EqualsNormalized(actual, expected),
            "contains" => ContainsNormalized(actual, expected),
            "isnotempty" => !string.IsNullOrWhiteSpace(actual),
            "greaterthan" => GreaterThanDecimal(actual, expected),
            _ => EqualsNormalized(actual, expected),
        };
    }

    private static bool EqualsNormalized(string? actual, string? expected)
    {
        return string.Equals(actual?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsNormalized(string? actual, string? expected)
    {
        return !string.IsNullOrEmpty(actual)
            && !string.IsNullOrEmpty(expected)
            && actual.Contains(expected.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool GreaterThanDecimal(string? actual, string? expected)
    {
        return decimal.TryParse(
                actual,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var a
            )
            && decimal.TryParse(
                expected,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var b
            )
            && a > b;
    }
}
