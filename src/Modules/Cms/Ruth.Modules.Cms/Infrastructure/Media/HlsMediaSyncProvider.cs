using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class HlsMediaSyncProvider(CmsMediaSyncRemoteFetcher? remoteFetcher = null)
    : IMediaAccountSyncProvider
{
    private readonly CmsMediaSyncRemoteFetcher _remoteFetcher = remoteFetcher
        ?? new CmsMediaSyncRemoteFetcher();

    public string ProviderKey => "hls";
    public string DisplayName => "HLS Manifest Sync";

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

        var manifestUrl =
            settings.TryGetValue("MasterManifestUrl", out var configured)
            && !string.IsNullOrWhiteSpace(configured)
                ? configured.Trim()
                : $"https://streams.example.org/{accountIdentifier.Trim()}/master.m3u8";

        SyncedMediaFolderSpec[] fallback =
        [
            new(
                $"hls-{accountIdentifier.Trim()}",
                "HLS Streams",
                "Adaptive bitrate HLS media folder.",
                [
                    new(
                        $"hls-{accountIdentifier.Trim()}-live",
                        "Primary HLS Stream",
                        "HLS adaptive stream manifest.",
                        900,
                        ThumbnailUrl: null,
                        manifestUrl,
                        manifestUrl,
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
