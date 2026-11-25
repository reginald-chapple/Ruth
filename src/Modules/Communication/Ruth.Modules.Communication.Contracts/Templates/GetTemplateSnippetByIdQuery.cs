using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.Templates;

/// <summary>Retrieves a single reusable template snippet by its identifier.</summary>
public sealed record GetTemplateSnippetByIdQuery(Guid SnippetId) : IQuery<TemplateSnippetResponse>;
