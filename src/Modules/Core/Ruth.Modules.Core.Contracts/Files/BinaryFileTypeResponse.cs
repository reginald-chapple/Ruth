namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Binary file type policy governing storage provider, size/dimension caps, and view security.</summary>
public sealed record BinaryFileTypeResponse(
    Guid Id,
    Guid SystemGuid,
    string Name,
    string? Description,
    string? IconName,
    string StorageProviderKey,
    bool RequiresViewSecurity,
    bool AllowAnonymous,
    bool PreferredRequired,
    long? MaxFileSizeBytes,
    int? MaxWidth,
    int? MaxHeight,
    string? AllowedExtensions,
    string? CacheControlHeaderSettings,
    bool IsSystem,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? ModifiedOnUtc
);
