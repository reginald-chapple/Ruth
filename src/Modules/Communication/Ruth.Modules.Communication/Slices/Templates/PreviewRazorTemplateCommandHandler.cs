using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class PreviewRazorTemplateCommandHandler(
    IRazorTemplateRenderer razorRenderer,
    IMergeTemplateEngine mergeEngine
) : ICommandHandler<PreviewRazorTemplateCommand, TemplatePreviewResponse>
{
    public async Task<Result<TemplatePreviewResponse>> HandleAsync(
        PreviewRazorTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var contextResult = MergeValueResolver.ParseJsonObject(command.MergeContextJson);
        if (contextResult.IsFailure)
        {
            return Result.Failure<TemplatePreviewResponse>(contextResult.Errors);
        }

        var resolvedParameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        var missingTokens = new List<string>();
        var snippetsExpanded = 0;

        if (command.Parameters is not null)
        {
            var expandResult = await ExpandParametersAsync(
                command.Parameters,
                contextResult.Value,
                resolvedParameters,
                missingTokens,
                cancellationToken
            );
            if (expandResult.IsFailure)
            {
                return Result.Failure<TemplatePreviewResponse>(expandResult.Errors);
            }

            snippetsExpanded = expandResult.Value;
        }

        var htmlResult = await razorRenderer.RenderNamedTemplateAsync(
            command.TemplateKey,
            resolvedParameters,
            cancellationToken
        );
        return htmlResult.IsFailure
            ? Result.Failure<TemplatePreviewResponse>(htmlResult.Errors)
            : new TemplatePreviewResponse(htmlResult.Value, missingTokens, snippetsExpanded);
    }

    private async Task<Result<int>> ExpandParametersAsync(
        IReadOnlyDictionary<string, string?> parameters,
        IReadOnlyDictionary<string, object?> mergeContext,
        Dictionary<string, object?> destination,
        List<string> missingTokens,
        CancellationToken cancellationToken
    )
    {
        var snippetsExpanded = 0;
        var hasMergeContext = mergeContext.Count > 0;

        foreach (var pair in parameters)
        {
            if (string.IsNullOrEmpty(pair.Value))
            {
                destination[pair.Key] = pair.Value;
                continue;
            }

            if (!hasMergeContext && !pair.Value.Contains("{{", StringComparison.Ordinal))
            {
                destination[pair.Key] = pair.Value;
                continue;
            }

            var outputMode = pair.Key.EndsWith("Html", StringComparison.OrdinalIgnoreCase)
                ? TemplateOutputMode.Html
                : TemplateOutputMode.PlainText;
            var merged = await mergeEngine.RenderAsync(
                pair.Value,
                mergeContext,
                new MergeRenderOptions(OutputMode: outputMode),
                cancellationToken
            );
            if (merged.IsFailure)
            {
                return Result.Failure<int>(merged.Errors);
            }

            destination[pair.Key] = merged.Value.RenderedContent;
            snippetsExpanded += merged.Value.SnippetsExpanded;
            AddUniqueTokens(missingTokens, merged.Value.MissingTokens);
        }

        return snippetsExpanded;
    }

    private static void AddUniqueTokens(List<string> destination, IReadOnlyList<string> source)
    {
        foreach (var token in source)
        {
            if (!destination.Contains(token, StringComparer.OrdinalIgnoreCase))
            {
                destination.Add(token);
            }
        }
    }
}
