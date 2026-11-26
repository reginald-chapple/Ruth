using Ruth.Modules.Cms.Contracts.Media;

namespace Ruth.Modules.Cms.Infrastructure.Media;

internal sealed class InMemoryMediaSyncProvider : IMediaAccountSyncProvider
{
    public string ProviderKey => "in-memory";
    public string DisplayName => "In-Memory Deterministic Media Provider";

    public Task<IReadOnlyList<SyncedMediaFolderSpec>> FetchFoldersAsync(
        string accountIdentifier,
        IReadOnlyDictionary<string, string?> settings,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var slug = string.IsNullOrWhiteSpace(accountIdentifier) ? "demo" : accountIdentifier.Trim();

        SyncedMediaFolderSpec[] folders =
        [
            new(
                $"{slug}-featured",
                "Featured Sessions",
                "Synced featured community media library.",
                [
                    new(
                        $"{slug}-vid-001",
                        "Community Welcome Overview",
                        "Orientation and member benefits walkthrough.",
                        245,
                        $"https://cdn.ruth.local/{slug}/welcome-thumb.jpg",
                        $"https://cdn.ruth.local/{slug}/welcome.mp4",
                        $"https://cdn.ruth.local/{slug}/welcome/master.m3u8",
                        $"https://cdn.ruth.local/{slug}/welcome.vtt",
                        1920,
                        1080,
                        new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero),
                        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["provider"] = ProviderKey,
                        }
                    ),
                    new(
                        $"{slug}-vid-002",
                        "Quarterly Impact Report",
                        "Highlights from programs, workshops, and volunteer initiatives.",
                        420,
                        $"https://cdn.ruth.local/{slug}/impact-thumb.jpg",
                        $"https://cdn.ruth.local/{slug}/impact.mp4",
                        $"https://cdn.ruth.local/{slug}/impact/master.m3u8",
                        CaptionsUrl: null,
                        1920,
                        1080,
                        new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero),
                        new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["provider"] = ProviderKey,
                        }
                    ),
                ]
            ),
        ];

        return Task.FromResult<IReadOnlyList<SyncedMediaFolderSpec>>(folders);
    }
}
