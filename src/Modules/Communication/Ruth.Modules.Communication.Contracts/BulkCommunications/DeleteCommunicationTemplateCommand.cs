using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.BulkCommunications;

/// <summary>Deletes a non-system CommunicationTemplate.</summary>
/// <param name="TemplateId">Identifier of the template to delete.</param>
public sealed record DeleteCommunicationTemplateCommand(Guid TemplateId) : ICommand<bool>;
