using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Workflow.Contracts.Actions;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Workflow.Slices.Actions;

internal sealed class RenderMergeOrRazorTemplateAction(
    IMergeTemplateEngine mergeEngine,
    IRazorTemplateRenderer razorRenderer
) : IWorkflowAction
{
    public const string Key = "workflow.templates.render-merge";

    public string ActionKey => Key;
    public string DisplayName => "Render Merge or Razor Template";
    public string Category => "Templates";
    public string Description =>
        "Renders TemplateMarkup (IMergeTemplateEngine) or RazorTemplateKey (IRazorTemplateRenderer) into TargetAttributeKey.";

    public async Task<Result<WorkflowActionDirective>> ExecuteAsync(
        WorkflowActionExecutionContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (
            !context.Configuration.TryGetValue("TargetAttributeKey", out var targetAttr)
            || string.IsNullOrWhiteSpace(targetAttr)
        )
        {
            return Result.Failure<WorkflowActionDirective>(
                DomainError.Validation(
                    "Workflow.TemplateMissingTarget",
                    "TargetAttributeKey is required for template rendering."
                )
            );
        }

        var renderRes = await RenderContentAsync(context, cancellationToken);
        if (renderRes.IsFailure)
        {
            return Result.Failure<WorkflowActionDirective>(renderRes.PrimaryError);
        }

        var updates = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [targetAttr.Trim()] = renderRes.Value,
        };

        return Result.Success(
            new WorkflowActionDirective(
                WorkflowActionOutcomeKind.Continue,
                $"Rendered template into '{targetAttr.Trim()}'.",
                UpdatedWorkflowAttributes: updates
            )
        );
    }

    private async Task<Result<string>> RenderContentAsync(
        WorkflowActionExecutionContext context,
        CancellationToken ct
    )
    {
        var mergeContext = BuildMergeContext(context);
        if (
            context.Configuration.TryGetValue("RazorTemplateKey", out var razorKey)
            && !string.IsNullOrWhiteSpace(razorKey)
        )
        {
            return await razorRenderer.RenderNamedTemplateAsync(razorKey.Trim(), mergeContext, ct);
        }

        _ = context.Configuration.TryGetValue("TemplateMarkup", out var markup);
        var mergeRes = await mergeEngine.RenderAsync(
            markup ?? string.Empty,
            mergeContext,
            options: null,
            ct
        );
        return mergeRes.IsFailure
            ? Result.Failure<string>(mergeRes.PrimaryError)
            : Result.Success(mergeRes.Value.RenderedContent);
    }

    private static Dictionary<string, object?> BuildMergeContext(
        WorkflowActionExecutionContext context
    )
    {
        var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["WorkflowId"] = context.WorkflowId.ToString("D"),
            ["WorkflowNumber"] = context.WorkflowNumber,
            ["WorkflowName"] = context.WorkflowName,
            ["StateLabel"] = context.StateLabel,
        };

        foreach (var (k, v) in context.WorkflowAttributes)
        {
            dict[k] = v;
        }

        return dict;
    }
}
