using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Lists reusable template snippets with optional active-only and search filtering.</summary>
public sealed record GetTemplateSnippetsQuery(bool ActiveOnly = false, string? Search = null)
    : IQuery<IReadOnlyList<TemplateSnippetResponse>>;
