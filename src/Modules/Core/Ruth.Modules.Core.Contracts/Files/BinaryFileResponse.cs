namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Metadata for a stored binary file.</summary>
public sealed record BinaryFileResponse(
    Guid Id,
    Guid SystemGuid,
    Guid BinaryFileTypeId,
    string FileName,
    string MimeType,
    long FileSize,
    string StorageProviderKey,
    string ObjectKey,
    int? Width,
    int? Height,
    bool IsTemporary,
    bool IsSystem,
    string? Description,
    Guid? ParentEntityTypeId,
    Guid? ParentEntityId,
    Guid? UploadedByUserId,
    DateTimeOffset? ContentLastModifiedOnUtc,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
