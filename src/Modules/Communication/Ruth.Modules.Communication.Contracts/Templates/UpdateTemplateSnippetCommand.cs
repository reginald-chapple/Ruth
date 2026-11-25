using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Updates an existing reusable template snippet.</summary>
public sealed record UpdateTemplateSnippetCommand(
    Guid SnippetId,
    string Name,
    string Key,
    string Markup,
    string? Description = null,
    string? Documentation = null,
    TemplateOutputMode OutputMode = TemplateOutputMode.Html,
    IReadOnlyDictionary<string, string>? DefaultParameters = null,
    IReadOnlyList<Guid>? CategoryIds = null,
    bool IsActive = true
) : ICommand<TemplateSnippetResponse>;
