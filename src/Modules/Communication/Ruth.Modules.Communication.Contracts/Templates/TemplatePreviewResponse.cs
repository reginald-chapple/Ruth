namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Rendered preview output returned by template preview endpoints and UI tools.</summary>
public sealed record TemplatePreviewResponse(
    string RenderedContent,
    IReadOnlyList<string> MissingTokens,
    int SnippetsExpanded
);
