using System.Text.Json;
using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class HlsMediaSyncProvider : IMediaAccountSyncProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string ProviderKey => "hls";
    public string DisplayName => "HLS Manifest Sync";

    public Task<IReadOnlyList<SyncedMediaFolderSpec>> FetchFoldersAsync(
        string accountIdentifier,
        IReadOnlyDictionary<string, string?> settings,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (
            settings.TryGetValue("ManifestJson", out var manifestJson)
            && !string.IsNullOrWhiteSpace(manifestJson)
        )
        {
            var parsed = JsonSerializer.Deserialize<List<SyncedMediaFolderSpec>>(
                manifestJson,
                _jsonOptions
            );
            if (parsed is not null)
            {
                return Task.FromResult<IReadOnlyList<SyncedMediaFolderSpec>>(parsed);
            }
        }

        var manifestUrl =
            settings.TryGetValue("MasterManifestUrl", out var configured)
            && !string.IsNullOrWhiteSpace(configured)
                ? configured.Trim()
                : $"https://streams.ruth.local/{accountIdentifier.Trim()}/master.m3u8";

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

        return Task.FromResult<IReadOnlyList<SyncedMediaFolderSpec>>(fallback);
    }
}
