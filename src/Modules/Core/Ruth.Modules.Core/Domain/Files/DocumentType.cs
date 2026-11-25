using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Entity-scoped document classification linked to a <see cref="BinaryFileType" />.</summary>
public sealed class DocumentType
    : AggregateRoot<DocumentTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for DocumentType.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.DocumentType";

    private DocumentType() { }

    private DocumentType(
        DocumentTypeId id,
        Guid systemGuid,
        string name,
        EntityTypeDescriptorId entityTypeId,
        BinaryFileTypeId binaryFileTypeId,
        string? qualifierColumn,
        string? qualifierValue,
        bool userSelectable,
        string? iconName,
        int order,
        string? defaultNameTemplate,
        int? maxDocumentsPerEntity,
        bool isImage,
        bool isSystem
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        Name = name;
        EntityTypeId = entityTypeId;
        BinaryFileTypeId = binaryFileTypeId;
        EntityTypeQualifierColumn = qualifierColumn;
        EntityTypeQualifierValue = qualifierValue;
        UserSelectable = userSelectable;
        IconName = iconName;
        Order = Math.Max(0, order);
        DefaultDocumentNameTemplate = defaultNameTemplate;
        MaxDocumentsPerEntity = maxDocumentsPerEntity is > 0 ? maxDocumentsPerEntity : null;
        IsImage = isImage;
        IsSystem = isSystem;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the document type display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the target entity type descriptor identifier.</summary>
    public EntityTypeDescriptorId EntityTypeId { get; private set; }

    /// <summary>Gets the optional qualifier property name.</summary>
    public string? EntityTypeQualifierColumn { get; private set; }

    /// <summary>Gets the optional qualifier property value.</summary>
    public string? EntityTypeQualifierValue { get; private set; }

    /// <summary>Gets the backing binary file type identifier.</summary>
    public BinaryFileTypeId BinaryFileTypeId { get; private set; }

    /// <summary>Gets whether staff may manually select this document type.</summary>
    public bool UserSelectable { get; private set; }

    /// <summary>Gets the optional icon name.</summary>
    public string? IconName { get; private set; }

    /// <summary>Gets the display sort order.</summary>
    public int Order { get; private set; }

    /// <summary>Gets the optional default document name template.</summary>
    public string? DefaultDocumentNameTemplate { get; private set; }

    /// <summary>Gets the optional maximum number of documents per entity (null = unlimited).</summary>
    public int? MaxDocumentsPerEntity { get; private set; }

    /// <summary>Gets whether documents of this type are images.</summary>
    public bool IsImage { get; private set; }

    /// <summary>Gets whether this document type is built-in.</summary>
    public bool IsSystem { get; private set; }

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
    public ISecured ParentAuthority => GlobalDefaultSecured.Instance;

    /// <inheritdoc />
    public bool IsAllowedByDefault(string action)
    {
        return GlobalDefaultSecured.Instance.IsAllowedByDefault(action);
    }

    /// <summary>Creates a validated document type.</summary>
    public static Result<DocumentType> Create(
        string name,
        EntityTypeDescriptorId entityTypeId,
        BinaryFileTypeId binaryFileTypeId,
        string? qualifierColumn = null,
        string? qualifierValue = null,
        bool userSelectable = true,
        string? iconName = null,
        int order = 0,
        string? defaultNameTemplate = null,
        int? maxDocumentsPerEntity = null,
        bool isImage = false,
        bool isSystem = false
    )
    {
        if (entityTypeId == EntityTypeDescriptorId.Empty)
        {
            return CoreErrors.EntityTypeNotFound;
        }

        if (binaryFileTypeId == BinaryFileTypeId.Empty)
        {
            return CoreDocumentErrors.BinaryFileTypeNotFound;
        }

        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreDocumentErrors.DocumentTypeNameRequired,
            CoreDocumentErrors.BinaryFileTypeNameTooLong
        );
        if (nameResult.IsFailure)
        {
            return Result.Failure<DocumentType>(nameResult.Errors);
        }

        var id = DocumentTypeId.Create();
        return new DocumentType(
            id,
            id.Value,
            nameResult.Value,
            entityTypeId,
            binaryFileTypeId,
            string.IsNullOrWhiteSpace(qualifierColumn) ? null : qualifierColumn.Trim(),
            string.IsNullOrWhiteSpace(qualifierValue) ? null : qualifierValue.Trim(),
            userSelectable,
            string.IsNullOrWhiteSpace(iconName) ? null : iconName.Trim(),
            order,
            string.IsNullOrWhiteSpace(defaultNameTemplate) ? null : defaultNameTemplate.Trim(),
            maxDocumentsPerEntity,
            isImage,
            isSystem
        );
    }
}
