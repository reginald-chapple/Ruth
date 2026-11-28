using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class VimeoMediaSyncProvider(CmsMediaSyncRemoteFetcher? remoteFetcher = null)
    : IMediaAccountSyncProvider
{
    private readonly CmsMediaSyncRemoteFetcher _remoteFetcher = remoteFetcher
        ?? new CmsMediaSyncRemoteFetcher();

    public string ProviderKey => "vimeo";
    public string DisplayName => "Vimeo Media Sync";

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
                $"vimeo-{id}-showcase",
                "Vimeo Showcase",
                $"Synced Vimeo showcase for account '{id}'.",
                [
                    new(
                        $"vimeo-{id}-1001",
                        "Vimeo Keynote Stream",
                        "HD HLS stream synced from Vimeo showcase.",
                        600,
                        $"https://i.vimeocdn.com/video/{id}_1280.jpg",
                        $"https://vimeo.com/{id}",
                        $"https://player.vimeo.com/external/{id}.m3u8",
                        CaptionsUrl: null,
                        1920,
                        1080,
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
