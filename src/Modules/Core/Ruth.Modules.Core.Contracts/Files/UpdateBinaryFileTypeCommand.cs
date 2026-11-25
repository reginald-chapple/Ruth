using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Updates a binary file type policy.</summary>
public sealed record UpdateBinaryFileTypeCommand(
    Guid BinaryFileTypeId,
    string Name,
    string? Description = null,
    string? IconName = null,
    string StorageProviderKey = "rustfs",
    bool RequiresViewSecurity = false,
    bool AllowAnonymous = false,
    bool PreferredRequired = false,
    long? MaxFileSizeBytes = null,
    int? MaxWidth = null,
    int? MaxHeight = null,
    string? AllowedExtensions = null,
    string? CacheControlHeaderSettings = null
) : ICommand<BinaryFileTypeResponse>;
