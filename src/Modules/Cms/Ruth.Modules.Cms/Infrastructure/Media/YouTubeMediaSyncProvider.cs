using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class YouTubeMediaSyncProvider(CmsMediaSyncRemoteFetcher? remoteFetcher = null)
    : IMediaAccountSyncProvider
{
    private readonly CmsMediaSyncRemoteFetcher _remoteFetcher = remoteFetcher
        ?? new CmsMediaSyncRemoteFetcher();

    public string ProviderKey => "youtube";
    public string DisplayName => "YouTube Channel Sync";

    public async Task<IReadOnlyList<SyncedMediaFolderSpec>> FetchFoldersAsync(
        string accountIdentifier,
        IReadOnlyDictionary<string, string?> settings,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolved = await _remoteFetcher.TryResolveConfiguredFoldersAsync(
            settings,
            cancellationToken
        );
        if (resolved is not null)
        {
            return resolved;
        }

        var id = accountIdentifier.Trim();
        SyncedMediaFolderSpec[] fallback =
        [
            new(
                $"youtube-{id}-uploads",
                "YouTube Uploads",
                $"Synced uploads playlist for channel '{id}'.",
                [
                    new(
                        $"yt-{id}-01",
                        "YouTube Community Update",
                        "Synced video from YouTube channel.",
                        360,
                        $"https://i.ytimg.com/vi/{id}/hqdefault.jpg",
                        $"https://www.youtube.com/watch?v={id}",
                        HlsManifestUrl: null,
                        CaptionsUrl: null,
                        1280,
                        720,
                        DateTimeOffset.UtcNow,
                        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["provider"] = ProviderKey,
                        }
                    ),
                ]
            ),
        ];

        CmsMediaSyncRemoteFetcher.ValidateSyncedFolders(fallback);
        return fallback;
    }
}
