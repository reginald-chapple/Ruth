using Ruth.Modules.Communication.Contracts.Templates;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Immutable runtime projection of an active reusable template snippet.</summary>
public sealed record TemplateSnippetDefinition(
    string Key,
    string Markup,
    TemplateOutputMode OutputMode,
    IReadOnlyDictionary<string, string> DefaultParameters
);
