namespace Ruth.Modules.Cms.Contracts.Media;

/// <summary>Modular DI-registered strategy contract for synchronizing Vimeo, YouTube, HLS, or in-memory media catalogs.</summary>
public interface IMediaAccountSyncProvider
{
    /// <summary>Gets the canonical provider key (e.g., vimeo, youtube, hls, in-memory).</summary>
    string ProviderKey { get; }

    /// <summary>Gets the human-readable provider display name.</summary>
    string DisplayName { get; }

    /// <summary>Fetches folders and media elements for the specified external media account.</summary>
    Task<IReadOnlyList<SyncedMediaFolderSpec>> FetchFoldersAsync(
        string accountIdentifier,
        IReadOnlyDictionary<string, string?> settings,
        CancellationToken cancellationToken = default
    );
}
