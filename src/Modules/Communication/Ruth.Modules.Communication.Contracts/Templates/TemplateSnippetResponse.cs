namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>API and UI representation of a reusable declarative template snippet.</summary>
public sealed record TemplateSnippetResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string Key,
    string? Description,
    string? Documentation,
    string Markup,
    TemplateOutputMode OutputMode,
    IReadOnlyDictionary<string, string> DefaultParameters,
    IReadOnlyList<Guid> CategoryIds,
    bool IsSystem,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
