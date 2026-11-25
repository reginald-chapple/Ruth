using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Renders a registered compiled Razor template with optional merge-token expansion on string parameters.</summary>
public sealed record PreviewRazorTemplateCommand(
    string TemplateKey,
    IReadOnlyDictionary<string, string?>? Parameters = null,
    string? MergeContextJson = null
) : ICommand<TemplatePreviewResponse>;
