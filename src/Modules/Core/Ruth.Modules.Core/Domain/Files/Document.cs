using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Document aggregate attaching a permanent <see cref="BinaryFile" /> to a target entity.</summary>
public sealed class Document : AggregateRoot<DocumentId>, IAuditableEntity, ISecured
{
    /// <summary>Canonical entity type key for Document.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.Document";

    private Document() { }

    private Document(
        DocumentId id,
        DocumentTypeId documentTypeId,
        Guid entityId,
        BinaryFileId binaryFileId,
        string name,
        string? purposeKey,
        string? description,
        bool isSystem
    )
        : base(id)
    {
        DocumentTypeId = documentTypeId;
        EntityId = entityId;
        BinaryFileId = binaryFileId;
        Name = name;
        PurposeKey = purposeKey;
        Description = description;
        IsSystem = isSystem;
    }

    /// <summary>Gets the owning document type identifier.</summary>
    public DocumentTypeId DocumentTypeId { get; private set; }

    /// <summary>Gets the target entity identifier this document belongs to.</summary>
    public Guid EntityId { get; private set; }

    /// <summary>Gets the attached binary file identifier.</summary>
    public BinaryFileId BinaryFileId { get; private set; }

    /// <summary>Gets the display name of the document.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the optional purpose key.</summary>
    public string? PurposeKey { get; private set; }

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets whether this document is system-protected.</summary>
    public bool IsSystem { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

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
    public string EntityTypeKey => EntityTypeKeyName;

    /// <inheritdoc />
    Guid ISecured.EntityId => Id.Value;

    /// <inheritdoc />
    public ISecured ParentAuthority =>
        new SecuredEntityRef(
            DocumentType.EntityTypeKeyName,
            DocumentTypeId.Value,
            GlobalDefaultSecured.Instance
        );

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated document record.</summary>
    public static Result<Document> Create(
        DocumentTypeId documentTypeId,
        Guid entityId,
        BinaryFileId binaryFileId,
        string name,
        string? purposeKey = null,
        string? description = null
    )
    {
        if (documentTypeId == DocumentTypeId.Empty)
        {
            return CoreDocumentErrors.DocumentTypeNotFound;
        }

        if (binaryFileId == BinaryFileId.Empty)
        {
            return CoreDocumentErrors.BinaryFileNotFound;
        }

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreDocumentErrors.DocumentNameRequired,
            CoreDocumentErrors.BinaryFileTypeNameTooLong
        );
        return nameResult.IsFailure
            ? Result.Failure<Document>(nameResult.Errors)
            : (Result<Document>)
                new Document(
                    DocumentId.Create(),
                    documentTypeId,
                    entityId,
                    binaryFileId,
                    nameResult.Value,
                    string.IsNullOrWhiteSpace(purposeKey) ? null : purposeKey.Trim(),
                    string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                    isSystem: false
                );
    }
}
