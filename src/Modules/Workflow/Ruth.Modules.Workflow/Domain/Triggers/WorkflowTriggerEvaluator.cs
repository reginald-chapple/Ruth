using System.Globalization;
using Ruth.Modules.Workflow.Contracts.Triggers;

namespace Ruth.Modules.Workflow.Domain.Triggers;

/// <summary>Pure deterministic predicate evaluator for WorkflowTrigger automation stimuli.</summary>
public static class WorkflowTriggerEvaluator
{
    public static bool Matches(
        WorkflowTrigger trigger,
        WorkflowTriggerKind stimulusKind,
        string eventTypeKey,
        string? entityTypeQualifier,
        IReadOnlyDictionary<string, string?>? payloadAttributes
    )
    {
        ArgumentNullException.ThrowIfNull(trigger);
        if (
            !MatchesHeader(trigger, stimulusKind, eventTypeKey)
            || !MatchesQualifier(trigger.EntityTypeQualifier, entityTypeQualifier)
        )
        {
            return false;
        }

        if (trigger.FilterOperator == WorkflowTriggerOperator.Always)
        {
            return true;
        }

        var actual = LookupCaseInsensitive(payloadAttributes, trigger.FilterAttributeKey);
        return EvaluateOperator(trigger.FilterOperator, actual, trigger.FilterExpectedValue);
    }

    private static bool MatchesHeader(
        WorkflowTrigger trigger,
        WorkflowTriggerKind stimulusKind,
        string eventTypeKey
    )
    {
        return trigger.IsActive
            && trigger.TriggerKind == stimulusKind
            && !string.IsNullOrWhiteSpace(eventTypeKey)
            && string.Equals(
                trigger.EventTypeKey,
                eventTypeKey.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static bool MatchesQualifier(string? expectedQualifier, string? actualQualifier)
    {
        return string.IsNullOrWhiteSpace(expectedQualifier)
            || string.Equals(
                expectedQualifier,
                actualQualifier?.Trim(),
                StringComparison.OrdinalIgnoreCase
            );
    }

    private static string? LookupCaseInsensitive(
        IReadOnlyDictionary<string, string?>? dict,
        string? key
    )
    {
        return dict is null || string.IsNullOrWhiteSpace(key) ? null
            : dict.TryGetValue(key, out var direct) ? direct
            : ScanCaseInsensitive(dict, key);
    }

    private static string? ScanCaseInsensitive(
        IReadOnlyDictionary<string, string?> dict,
        string key
    )
    {
        foreach (var (k, v) in dict)
        {
            if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
            {
                return v;
            }
        }

        return null;
    }

    private static bool EvaluateOperator(
        WorkflowTriggerOperator op,
        string? actual,
        string? expected
    )
    {
        return op switch
        {
            WorkflowTriggerOperator.Always => true,
            WorkflowTriggerOperator.Equals => EqualsTrimmed(actual, expected),
            WorkflowTriggerOperator.NotEquals => !EqualsTrimmed(actual, expected),
            WorkflowTriggerOperator.Contains => ContainsTrimmed(actual, expected),
            WorkflowTriggerOperator.IsNotEmpty => !string.IsNullOrWhiteSpace(actual),
            WorkflowTriggerOperator.GreaterThan => GreaterThanDecimal(actual, expected),
            _ => false,
        };
    }

    private static bool EqualsTrimmed(string? actual, string? expected)
    {
        return string.Equals(actual?.Trim(), expected?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsTrimmed(string? actual, string? expected)
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
                out var left
            )
            && decimal.TryParse(
                expected,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var right
            )
            && left > right;
    }
}
