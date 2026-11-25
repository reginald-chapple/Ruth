using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Creates a new document type scoped to an entity type and binary file type.</summary>
public sealed record CreateDocumentTypeCommand(
    string Name,
    Guid EntityTypeId,
    Guid BinaryFileTypeId,
    string? EntityTypeQualifierColumn = null,
    string? EntityTypeQualifierValue = null,
    bool UserSelectable = true,
    string? IconName = null,
    int Order = 0,
    string? DefaultDocumentNameTemplate = null,
    int? MaxDocumentsPerEntity = null,
    bool IsImage = false
) : ICommand<DocumentTypeResponse>;
