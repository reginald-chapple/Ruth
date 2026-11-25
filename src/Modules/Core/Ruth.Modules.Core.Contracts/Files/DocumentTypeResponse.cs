namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Entity-scoped document type template linked to a BinaryFileType.</summary>
public sealed record DocumentTypeResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    Guid EntityTypeId,
    string? EntityTypeQualifierColumn,
    string? EntityTypeQualifierValue,
    Guid BinaryFileTypeId,
    bool UserSelectable,
    string? IconName,
    int Order,
    string? DefaultDocumentNameTemplate,
    int? MaxDocumentsPerEntity,
    bool IsImage,
    bool IsSystem,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
