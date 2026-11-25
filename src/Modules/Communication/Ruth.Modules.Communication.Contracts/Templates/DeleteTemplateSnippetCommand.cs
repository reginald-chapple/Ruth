using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Deletes a non-system template snippet.</summary>
public sealed record DeleteTemplateSnippetCommand(Guid SnippetId) : ICommand<bool>;
