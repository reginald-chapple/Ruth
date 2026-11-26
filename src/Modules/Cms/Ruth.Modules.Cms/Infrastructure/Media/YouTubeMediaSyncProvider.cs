using System.Text.Json;
using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class YouTubeMediaSyncProvider : IMediaAccountSyncProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string ProviderKey => "youtube";
    public string DisplayName => "YouTube Channel Sync";

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

        return Task.FromResult<IReadOnlyList<SyncedMediaFolderSpec>>(fallback);
    }
}
