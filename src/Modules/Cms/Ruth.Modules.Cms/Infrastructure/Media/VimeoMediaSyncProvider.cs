using System.Text.Json;
using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class VimeoMediaSyncProvider : IMediaAccountSyncProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public string ProviderKey => "vimeo";
    public string DisplayName => "Vimeo Media Sync";

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

        return Task.FromResult<IReadOnlyList<SyncedMediaFolderSpec>>(fallback);
    }
}
