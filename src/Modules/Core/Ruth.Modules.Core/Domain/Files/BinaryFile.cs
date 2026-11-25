using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Stored binary file aggregate referencing an object in a modular IStorageProvider.</summary>
public sealed class BinaryFile
    : AggregateRoot<BinaryFileId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for BinaryFile.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.BinaryFile";

    private BinaryFile() { }

    private BinaryFile(
        BinaryFileId id,
        Guid systemGuid,
        BinaryFileTypeId binaryFileTypeId,
        string fileName,
        string mimeType,
        long fileSize,
        string storageProviderKey,
        string objectKey,
        int? width,
        int? height,
        bool isTemporary,
        bool isSystem,
        string? description,
        EntityTypeDescriptorId? parentEntityTypeId,
        Guid? parentEntityId,
        Guid? uploadedByUserId,
        DateTimeOffset nowUtc
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        BinaryFileTypeId = binaryFileTypeId;
        FileName = fileName;
        MimeType = mimeType;
        FileSize = fileSize;
        StorageProviderKey = storageProviderKey;
        ObjectKey = objectKey;
        Width = width;
        Height = height;
        IsTemporary = isTemporary;
        IsSystem = isSystem;
        Description = description;
        ParentEntityTypeId = parentEntityTypeId;
        ParentEntityId = parentEntityId;
        UploadedByUserId = uploadedByUserId;
        ContentLastModifiedOnUtc = nowUtc;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the owning binary file type identifier.</summary>
    public BinaryFileTypeId BinaryFileTypeId { get; private set; }

    /// <summary>Gets the scrubbed file name.</summary>
    public string FileName { get; private set; } = string.Empty;

    /// <summary>Gets the MIME content type.</summary>
    public string MimeType { get; private set; } = "application/octet-stream";

    /// <summary>Gets the stored content length in bytes.</summary>
    public long FileSize { get; private set; }

    /// <summary>Gets the snapshot of the storage provider key used when saving this object.</summary>
    public string StorageProviderKey { get; private set; } = string.Empty;

    /// <summary>Gets the provider object key.</summary>
    public string ObjectKey { get; private set; } = string.Empty;

    /// <summary>Gets image width in pixels when applicable.</summary>
    public int? Width { get; private set; }

    /// <summary>Gets image height in pixels when applicable.</summary>
    public int? Height { get; private set; }

    /// <summary>Gets whether this file is temporary and eligible for cleanup until attached.</summary>
    public bool IsTemporary { get; private set; } = true;

    /// <summary>Gets whether this file is system-protected.</summary>
    public bool IsSystem { get; private set; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional parent entity type descriptor identifier.</summary>
    public EntityTypeDescriptorId? ParentEntityTypeId { get; private set; }

    /// <summary>Gets the optional parent entity identifier.</summary>
    public Guid? ParentEntityId { get; private set; }

    /// <summary>Gets the user account identifier that uploaded this file.</summary>
    public Guid? UploadedByUserId { get; private set; }

    /// <summary>Gets when the stored binary bytes were last modified.</summary>
    public DateTimeOffset? ContentLastModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }

    /// <inheritdoc />
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    public Guid EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            BinaryFileType.EntityTypeKeyName,
            BinaryFileTypeId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Gets whether this file has an image MIME type.</summary>
    public bool IsImage => MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>Checks whether <paramref name="requestingUserId" /> is allowed to attach this file to an entity.</summary>
    public bool CanBeAttachedBy(Guid? requestingUserId)
    {
        return IsTemporary
            && (
                !UploadedByUserId.HasValue
                || !requestingUserId.HasValue
                || UploadedByUserId.Value == requestingUserId.Value
            );
    }

    /// <summary>Marks the file as permanent once attached to a domain entity.</summary>
    public void MarkPermanent()
    {
        IsTemporary = false;
    }

    /// <summary>Marks the file as temporary so background cleanup can reclaim it.</summary>
    public void MarkTemporary()
    {
        IsTemporary = true;
    }

    /// <summary>Updates stored content metadata after an in-place crop or resize operation.</summary>
    public void UpdateStoredContent(
        string objectKey,
        string mimeType,
        long fileSize,
        int? width,
        int? height,
        DateTimeOffset modifiedUtc
    )
    {
        ObjectKey = objectKey;
        MimeType = mimeType;
        FileSize = fileSize;
        Width = width;
        Height = height;
        ContentLastModifiedOnUtc = modifiedUtc;
    }

    internal static Result<BinaryFile> Create(
        BinaryFileType fileType,
        string rawFileName,
        string mimeType,
        long fileSize,
        string storageProviderKey,
        string objectKey,
        int? width,
        int? height,
        bool isTemporary,
        string? description,
        EntityTypeDescriptorId? parentEntityTypeId,
        Guid? parentEntityId,
        Guid? uploadedByUserId,
        DateTimeOffset nowUtc
    )
    {
        var nameResult = BinaryFileNamePolicy.ScrubAndValidate(
            rawFileName,
            fileType.AllowedExtensions
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure<BinaryFile>(nameResult.Errors);
        }

        if (fileType.MaxFileSizeBytes.HasValue && fileSize > fileType.MaxFileSizeBytes.Value)
        {
            return CoreDocumentErrors.BinaryFileTooLarge;
        }

        var effectiveTemporary = !uploadedByUserId.HasValue || isTemporary;
        var id = BinaryFileId.Create();
        var normalizedMime = string.IsNullOrWhiteSpace(mimeType)
            ? "application/octet-stream"
            : mimeType.Trim();

        return new BinaryFile(
            id,
            id.Value,
            fileType.Id,
            nameResult.Value,
            normalizedMime,
            fileSize,
            storageProviderKey,
            objectKey,
            width,
            height,
            effectiveTemporary,
            isSystem: false,
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            parentEntityTypeId,
            parentEntityId,
            uploadedByUserId,
            nowUtc
        );
    }
}
