using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Attaches a binary file as a permanent document on an entity, enforcing per-entity quotas.</summary>
public sealed record CreateDocumentCommand(
    Guid DocumentTypeId,
    Guid EntityId,
    Guid BinaryFileId,
    string Name,
    string? PurposeKey = null,
    string? Description = null,
    Guid? RequestingUserId = null
) : ICommand<DocumentResponse>;
