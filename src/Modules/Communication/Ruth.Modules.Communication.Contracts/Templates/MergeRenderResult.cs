namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Outcome of evaluating a safe merge template.</summary>
public sealed record MergeRenderResult(
    string RenderedContent,
    IReadOnlyList<string> MissingTokens,
    int SnippetsExpanded
);
