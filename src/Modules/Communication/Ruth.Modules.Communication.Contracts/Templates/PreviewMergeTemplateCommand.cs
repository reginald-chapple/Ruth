using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Renders a merge template preview against sample JSON context data.</summary>
public sealed record PreviewMergeTemplateCommand(
    string Template,
    string? ContextJson = null,
    TemplateOutputMode OutputMode = TemplateOutputMode.Html,
    bool StrictMissingTokens = false
) : ICommand<TemplatePreviewResponse>;
