using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Uploads a binary file stream to the configured storage provider.</summary>
public sealed record UploadBinaryFileCommand(
    Guid BinaryFileTypeId,
    string FileName,
    string MimeType,
    Stream Content,
    bool IsTemporary = true,
    string? Description = null,
    Guid? ParentEntityTypeId = null,
    Guid? ParentEntityId = null,
    Guid? UploadedByUserId = null
) : ICommand<BinaryFileResponse>;
