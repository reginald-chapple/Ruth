using System.Text.Json;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.SharedKernel.Security;

namespace Ruth.Modules.Cms.Infrastructure.Media;

/// <summary>
/// Shared SSRF-guarded remote JSON/manifest fetcher and URL validator for CMS media account synchronization.
/// </summary>
internal sealed class CmsMediaSyncRemoteFetcher(IHttpClientFactory? httpClientFactory = null)
{
    public const string HttpClientName = "CmsMediaSyncHttpClient";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<SyncedMediaFolderSpec>?> TryResolveConfiguredFoldersAsync(
        IReadOnlyDictionary<string, string?> settings,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(settings);

        ValidateOptionalSettingUri(settings, "MasterManifestUrl");
        ValidateOptionalSettingUri(settings, "FeedUrl");
        ValidateOptionalSettingUri(settings, "RemoteManifestUrl");
        ValidateOptionalSettingUri(settings, "ApiEndpointUrl");

        if (TryGetRemoteFeedUrl(settings, out var remoteFeedUri) && httpClientFactory is not null)
        {
            OutboundAddressGuard.EnsureAllowedUri(remoteFeedUri);
            using var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(remoteFeedUri, cancellationToken);
            _ = response.EnsureSuccessStatusCode();
            var remoteJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var remoteFolders = DeserializeAndValidate(remoteJson);
            if (remoteFolders is not null)
            {
                return remoteFolders;
            }
        }

        return
            settings.TryGetValue("ManifestJson", out var manifestJson)
            && !string.IsNullOrWhiteSpace(manifestJson)
            ? DeserializeAndValidate(manifestJson)
            : null;
    }

    public static void ValidateSyncedFolders(IReadOnlyList<SyncedMediaFolderSpec> folders)
    {
        foreach (var folder in folders)
        {
            foreach (var element in folder.Elements)
            {
                ValidateOptionalElementUri(element.PlaybackUrl);
                ValidateOptionalElementUri(element.HlsManifestUrl);
                ValidateOptionalElementUri(element.ThumbnailUrl);
                ValidateOptionalElementUri(element.CaptionsUrl);
            }
        }
    }

    private static List<SyncedMediaFolderSpec>? DeserializeAndValidate(string json)
    {
        var parsed = JsonSerializer.Deserialize<List<SyncedMediaFolderSpec>>(json, _jsonOptions);
        if (parsed is not null)
        {
            ValidateSyncedFolders(parsed);
        }

        return parsed;
    }

    private static bool TryGetRemoteFeedUrl(
        IReadOnlyDictionary<string, string?> settings,
        out Uri remoteUri
    )
    {
        remoteUri = null!;
        string? raw = null;
        if (settings.TryGetValue("FeedUrl", out var feed) && !string.IsNullOrWhiteSpace(feed))
        {
            raw = feed;
        }
        else if (
            settings.TryGetValue("RemoteManifestUrl", out var remote)
            && !string.IsNullOrWhiteSpace(remote)
        )
        {
            raw = remote;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var parsed))
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.InvalidUri",
                "Configured media sync FeedUrl must be an absolute HTTP or HTTPS URL."
            );
        }

        remoteUri = parsed;
        return true;
    }

    private static void ValidateOptionalSettingUri(
        IReadOnlyDictionary<string, string?> settings,
        string key
    )
    {
        if (!settings.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        if (!Uri.TryCreate(raw.Trim(), UriKind.Absolute, out var parsed))
        {
            throw new OutboundSsrfBlockedException(
                "Outbound.InvalidUri",
                $"Configured media sync '{key}' must be an absolute HTTP or HTTPS URL."
            );
        }

        OutboundAddressGuard.EnsureAllowedUri(parsed);
    }

    private static void ValidateOptionalElementUri(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
        {
            return;
        }

        if (Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var parsed))
        {
            OutboundAddressGuard.EnsureAllowedUri(parsed);
        }
    }
}
