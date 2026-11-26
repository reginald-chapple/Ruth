using System.Globalization;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class SetWorkflowStatusControlAction : IWorkflowAction
{
    public const string Key = "workflow.control.set-status";

    public string ActionKey => Key;
    public string DisplayName => "Set Status, Delay or Complete";
    public string Category => "Control Flow";
    public string Description =>
        "Updates StateLabel and optionally delays execution (DelaySeconds), completes (CompleteWorkflow=true), or cancels (CancelWorkflow=true).";

    public Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult(Result.Success(BuildDirective(context)));
    }

    private static WorkflowActionDirective BuildDirective(WorkflowActionExecutionContext context)
    {
        var cfg = context.Configuration;
        _ = cfg.TryGetValue("StateLabel", out var stateLabel);

        var terminal = TryBuildTerminalDirective(cfg, stateLabel);
        if (terminal is not null)
        {
            return terminal;
        }

        var delayed = TryBuildDelayDirective(context.NowUtc, cfg, stateLabel);
        return delayed ?? BuildContinueDirective(stateLabel);
    }

    private static WorkflowActionDirective? TryBuildTerminalDirective(
        IReadOnlyDictionary<string, string?> cfg,
        string? stateLabel
    )
    {
        return IsTrue(cfg, "CancelWorkflow")
                ? new WorkflowActionDirective(
                    WorkflowActionOutcomeKind.CancelWorkflow,
                    $"Control action cancelled workflow ({stateLabel ?? "Cancelled"}).",
                    NewStateLabel: stateLabel
                )
            : IsTrue(cfg, "CompleteWorkflow")
                ? new WorkflowActionDirective(
                    WorkflowActionOutcomeKind.CompleteWorkflow,
                    $"Control action completed workflow ({stateLabel ?? "Completed"}).",
                    NewStateLabel: stateLabel
                )
            : null;
    }

    private static WorkflowActionDirective? TryBuildDelayDirective(
        DateTimeOffset nowUtc,
        IReadOnlyDictionary<string, string?> cfg,
        string? stateLabel
    )
    {
        if (
            !cfg.TryGetValue("DelaySeconds", out var rawDelay)
            || !int.TryParse(rawDelay, CultureInfo.InvariantCulture, out var seconds)
            || seconds <= 0
        )
        {
            return null;
        }

        var resumeAt = nowUtc.AddSeconds(seconds);
        return new WorkflowActionDirective(
            WorkflowActionOutcomeKind.DelayUntil,
            string.Create(
                CultureInfo.InvariantCulture,
                $"Delayed workflow for {seconds}s until {resumeAt:O}."
            ),
            NewStateLabel: stateLabel,
            ResumeAtUtc: resumeAt
        );
    }

    private static WorkflowActionDirective BuildContinueDirective(string? stateLabel)
    {
        return new WorkflowActionDirective(
            WorkflowActionOutcomeKind.Continue,
            string.IsNullOrWhiteSpace(stateLabel)
                ? "Evaluated status control step."
                : $"Updated state label to '{stateLabel}'.",
            NewStateLabel: stateLabel
        );
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string?> cfg, string key)
    {
        return cfg.TryGetValue(key, out var val)
            && !string.IsNullOrWhiteSpace(val)
            && string.Equals(val.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }
}
