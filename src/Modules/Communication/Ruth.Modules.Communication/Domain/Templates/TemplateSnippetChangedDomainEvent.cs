using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Communication.Domain.Templates;

/// <summary>Domain event raised when a reusable template snippet is created, updated, or deleted.</summary>
public sealed record TemplateSnippetChangedDomainEvent(
    TemplateSnippetId SnippetId,
    string Key,
    string ChangeKind
) : DomainEvent;
