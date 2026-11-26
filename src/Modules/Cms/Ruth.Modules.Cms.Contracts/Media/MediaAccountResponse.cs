namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Represents a configured external media account (Vimeo, YouTube, HLS, or In-Memory).</summary>
public sealed record MediaAccountResponse(
    Guid Id,
    string Name,
    string Slug,
    string ProviderKey,
    string AccountIdentifier,
    bool IsActive,
    DateTimeOffset? LastSyncedUtc,
    int FolderCount,
    int ElementCount
);
