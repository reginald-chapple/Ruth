using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Deletes a document and marks its linked binary file temporary for cleanup.</summary>
public sealed record DeleteDocumentCommand(Guid DocumentId) : ICommand<bool>;
