namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Represents a synchronized media folder inside a MediaAccount.</summary>
public sealed record MediaFolderResponse(
    Guid Id,
    Guid MediaAccountId,
    string Name,
    string ExternalFolderKey,
    string? Description,
    Guid? ContentChannelId,
    bool IsPublic,
    bool IsContentChannelSyncEnabled,
    int ElementCount
);
