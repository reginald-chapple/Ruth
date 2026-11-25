using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Core.Domain.Files;

/// <summary>Storage, security, and media constraints policy for a class of binary files.</summary>
public sealed class BinaryFileType
    : AggregateRoot<BinaryFileTypeId>,
        IAuditableEntity,
        IHasConcurrencyToken,
        ISecured
{
    /// <summary>Canonical entity type key for BinaryFileType.</summary>
    public const string EntityTypeKeyName = "Ruth.Core.BinaryFileType";

    private BinaryFileType() { }

    private BinaryFileType(BinaryFileTypeId id, Guid systemGuid, bool isSystem)
        : base(id)
    {
        SystemGuid = systemGuid;
        IsSystem = isSystem;
    }

    /// <summary>Gets the stable external/system GUID.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the unique display name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the uppercase normalized name for uniqueness checks.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>Gets the optional description.</summary>
    public string? Description { get; private set; }

    /// <summary>Gets the optional icon name.</summary>
    public string? IconName { get; private set; }

    /// <summary>Gets the storage provider key (e.g., rustfs).</summary>
    public string StorageProviderKey { get; private set; } = "rustfs";

    /// <summary>Gets whether per-file VIEW security ACL evaluation is required.</summary>
    public bool RequiresViewSecurity { get; private set; }

    /// <summary>Gets whether anonymous uploads are permitted (always marked temporary).</summary>
    public bool AllowAnonymous { get; private set; }

    /// <summary>Gets whether max size/dimension constraints are strictly enforced on upload.</summary>
    public bool PreferredRequired { get; private set; }

    /// <summary>Gets the optional maximum file size in bytes.</summary>
    public long? MaxFileSizeBytes { get; private set; }

    /// <summary>Gets the optional maximum image width in pixels.</summary>
    public int? MaxWidth { get; private set; }

    /// <summary>Gets the optional maximum image height in pixels.</summary>
    public int? MaxHeight { get; private set; }

    /// <summary>Gets an optional comma-separated allowlist of file extensions (e.g., .png,.jpg,.pdf).</summary>
    public string? AllowedExtensions { get; private set; }

    /// <summary>Gets optional JSON cache-control header configuration.</summary>
    public string? CacheControlHeaderSettings { get; private set; }

    /// <summary>Gets whether this binary file type is built-in and protected from deletion.</summary>
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

    /// <summary>Creates a validated binary file type.</summary>
    public static Result<BinaryFileType> Create(
        string name,
        string? description = null,
        string? iconName = null,
        string storageProviderKey = "rustfs",
        bool requiresViewSecurity = false,
        bool allowAnonymous = false,
        bool preferredRequired = false,
        long? maxFileSizeBytes = null,
        int? maxWidth = null,
        int? maxHeight = null,
        string? allowedExtensions = null,
        string? cacheControlHeaderSettings = null,
        bool isSystem = false,
        Guid? systemGuid = null
    )
    {
        var id = BinaryFileTypeId.Create();
        var item = new BinaryFileType(
            id,
            systemGuid.HasValue && systemGuid.Value != Guid.Empty ? systemGuid.Value : id.Value,
            isSystem
        );
        var apply = item.Update(
            name,
            description,
            iconName,
            storageProviderKey,
            requiresViewSecurity,
            allowAnonymous,
            preferredRequired,
            maxFileSizeBytes,
            maxWidth,
            maxHeight,
            allowedExtensions,
            cacheControlHeaderSettings
        );
        return apply.IsFailure
            ? Result.Failure<BinaryFileType>(apply.Errors)
            : Result.Success(item);
    }

    /// <summary>Updates binary file type settings.</summary>
    public Result Update(
        string name,
        string? description,
        string? iconName,
        string storageProviderKey,
        bool requiresViewSecurity,
        bool allowAnonymous,
        bool preferredRequired,
        long? maxFileSizeBytes,
        int? maxWidth,
        int? maxHeight,
        string? allowedExtensions,
        string? cacheControlHeaderSettings
    )
    {
        var nameResult = CoreTextPolicy.ValidateRequiredText(
            name,
            CoreTextPolicy.NameMaxLength,
            CoreDocumentErrors.BinaryFileTypeNameRequired,
            CoreDocumentErrors.BinaryFileTypeNameTooLong
        );
        var descResult = CoreTextPolicy.ValidateOptionalText(
            description,
            CoreTextPolicy.DescriptionMaxLength
        );
        var iconResult = CoreTextPolicy.ValidateOptionalText(iconName, 100, allowNewLines: false);
        var combined = Result.Combine(nameResult, descResult, iconResult);
        if (combined.IsFailure)
        {
            return combined;
        }

        Name = nameResult.Value;
        NormalizedName = Name.ToUpperInvariant();
        Description = descResult.Value;
        IconName = iconResult.Value;
        StorageProviderKey = string.IsNullOrWhiteSpace(storageProviderKey)
            ? "rustfs"
            : storageProviderKey.Trim().ToLowerInvariant();
        RequiresViewSecurity = requiresViewSecurity;
        AllowAnonymous = allowAnonymous;
        PreferredRequired = preferredRequired;
        MaxFileSizeBytes = maxFileSizeBytes is > 0 ? maxFileSizeBytes : null;
        MaxWidth = maxWidth is > 0 ? maxWidth : null;
        MaxHeight = maxHeight is > 0 ? maxHeight : null;
        AllowedExtensions = string.IsNullOrWhiteSpace(allowedExtensions)
            ? null
            : allowedExtensions.Trim();
        CacheControlHeaderSettings = string.IsNullOrWhiteSpace(cacheControlHeaderSettings)
            ? null
            : cacheControlHeaderSettings.Trim();
        return Result.Success();
    }
}
