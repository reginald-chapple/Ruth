namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Summary metadata for a registered compiled Razor template.</summary>
public sealed record RazorTemplateSummaryResponse(
    string Key,
    string DisplayName,
    string Description,
    string Category,
    string ComponentTypeName,
    IReadOnlyList<RazorTemplateParameterInfo> Parameters
);
