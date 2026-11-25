namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Execution bounds and formatting options for safe merge template rendering.</summary>
public sealed record MergeRenderOptions(
    TemplateOutputMode OutputMode = TemplateOutputMode.Html,
    string? CultureName = null,
    int MaxTemplateLength = 131_072,
    int MaxOutputLength = 524_288,
    int MaxTokenCount = 1_000,
    int MaxSnippetDepth = 4,
    int MaxConditionalDepth = 6,
    bool StrictMissingTokens = false
)
{
    /// <summary>Default HTML merge render options.</summary>
    public static MergeRenderOptions DefaultHtml { get; } = new(TemplateOutputMode.Html);

    /// <summary>Default plain-text merge render options.</summary>
    public static MergeRenderOptions DefaultPlainText { get; } = new(TemplateOutputMode.PlainText);
}
