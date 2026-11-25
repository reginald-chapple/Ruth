using Ruth.Modules.Communication.Contracts.Templates;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.Templates;

internal sealed class PreviewMergeTemplateCommandHandler(IMergeTemplateEngine mergeEngine)
    : ICommandHandler<PreviewMergeTemplateCommand, TemplatePreviewResponse>
{
    public async Task<Result<TemplatePreviewResponse>> HandleAsync(
        PreviewMergeTemplateCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var contextResult = MergeValueResolver.ParseJsonObject(command.ContextJson);
        if (contextResult.IsFailure)
        {
            return Result.Failure<TemplatePreviewResponse>(contextResult.Errors);
        }

        var options = new MergeRenderOptions(
            OutputMode: command.OutputMode,
            StrictMissingTokens: command.StrictMissingTokens
        );
        var rendered = await mergeEngine.RenderAsync(
            command.Template ?? string.Empty,
            contextResult.Value,
            options,
            cancellationToken
        );
        return rendered.IsFailure
            ? Result.Failure<TemplatePreviewResponse>(rendered.Errors)
            : new TemplatePreviewResponse(
                rendered.Value.RenderedContent,
                rendered.Value.MissingTokens,
                rendered.Value.SnippetsExpanded
            );
    }
}
