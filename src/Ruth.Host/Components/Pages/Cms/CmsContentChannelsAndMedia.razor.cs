using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Cms;

/// <summary>Staff W6-S2 studio for ContentChannels, MediaAccount sync, Bookmarks, and PersistedDatasets.</summary>
public sealed partial class CmsContentChannelsAndMedia : ComponentBase
{
    private IReadOnlyList<ContentChannelResponse> _channels = [];
    private IReadOnlyList<MediaAccountResponse> _mediaAccounts = [];
    private IReadOnlyList<PersistedDatasetResponse> _datasets = [];

    private string _channelName = "Community Stories";
    private string _channelSlug = "community-stories";
    private string _itemTitle = "Spring Impact Showcase";
    private string _itemSlug = "spring-impact-showcase";
    private string _mediaName = "Community Video Library";
    private string _mediaProvider = "in-memory";
    private string _datasetKey = "public-stories-feed";
    private string _datasetName = "Published Stories Dataset";
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        var chRes = await QueryDispatcher.QueryAsync<
            GetContentChannelsQuery,
            IReadOnlyList<ContentChannelResponse>
        >(new GetContentChannelsQuery());
        _channels = chRes.IsSuccess ? chRes.Value : [];

        var mediaRes = await QueryDispatcher.QueryAsync<
            GetMediaAccountsQuery,
            IReadOnlyList<MediaAccountResponse>
        >(new GetMediaAccountsQuery());
        _mediaAccounts = mediaRes.IsSuccess ? mediaRes.Value : [];

        var dsRes = await QueryDispatcher.QueryAsync<
            GetPersistedDatasetsQuery,
            IReadOnlyList<PersistedDatasetResponse>
        >(new GetPersistedDatasetsQuery());
        _datasets = dsRes.IsSuccess ? dsRes.Value : [];
    }

    private async Task CreateChannelAsync()
    {
        var typesRes = await QueryDispatcher.QueryAsync<
            GetContentChannelTypesQuery,
            IReadOnlyList<ContentChannelTypeResponse>
        >(new GetContentChannelTypesQuery());

        Guid typeId;
        if (typesRes.IsSuccess && typesRes.Value.Count > 0)
        {
            typeId = typesRes.Value[0].Id;
        }
        else
        {
            var createdType = await CommandDispatcher.SendAsync<
                CreateContentChannelTypeCommand,
                ContentChannelTypeResponse
            >(new CreateContentChannelTypeCommand("Articles", "articles"));
            if (createdType.IsFailure)
            {
                SetStatus(ok: false, createdType.PrimaryError.Description);
                return;
            }

            typeId = createdType.Value.Id;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateContentChannelCommand,
            ContentChannelResponse
        >(new CreateContentChannelCommand(typeId, _channelName, _channelSlug));
        SetStatus(
            res.IsSuccess,
            res.IsSuccess ? $"Created channel '{res.Value.Slug}'." : res.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private async Task PublishItemAsync()
    {
        if (_channels.Count == 0)
        {
            SetStatus(ok: false, "Create a content channel first.");
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            CreateContentChannelItemCommand,
            ContentChannelItemResponse
        >(
            new CreateContentChannelItemCommand(
                _channels[0].Id,
                _itemTitle,
                _itemSlug,
                "<p>Published content item from Ruth CMS Studio.</p>",
                Summary: "Community update"
            )
        );
        SetStatus(
            res.IsSuccess,
            res.IsSuccess ? $"Published item '{res.Value.Title}'." : res.PrimaryError.Description
        );
    }

    private async Task CreateAndSyncMediaAsync()
    {
        var slug = $"{_mediaProvider}-{Guid.CreateVersion7().ToString("N")[..6]}";
        var accRes = await CommandDispatcher.SendAsync<
            CreateMediaAccountCommand,
            MediaAccountResponse
        >(new CreateMediaAccountCommand(_mediaName, slug, _mediaProvider, "org-media"));
        if (accRes.IsFailure)
        {
            SetStatus(ok: false, accRes.PrimaryError.Description);
            return;
        }

        var syncRes = await CommandDispatcher.SendAsync<
            SyncMediaAccountCommand,
            MediaAccountResponse
        >(new SyncMediaAccountCommand(accRes.Value.Id));
        SetStatus(
            syncRes.IsSuccess,
            syncRes.IsSuccess
                ? string.Create(
                    CultureInfo.InvariantCulture,
                    $"Synced {syncRes.Value.ElementCount} media element(s) across {syncRes.Value.FolderCount} folder(s)."
                )
                : syncRes.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private async Task CreateDatasetAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreatePersistedDatasetCommand,
            PersistedDatasetResponse
        >(
            new CreatePersistedDatasetCommand(
                _datasetKey,
                _datasetName,
                PersistedDatasetSourceKind.ContentChannelFeed
            )
        );
        SetStatus(
            res.IsSuccess,
            res.IsSuccess
                ? $"Materialized dataset '{res.Value.AccessKey}'."
                : res.PrimaryError.Description
        );
        await ReloadAsync();
    }

    private void SetStatus(bool ok, string message)
    {
        _isError = !ok;
        _statusMessage = message;
    }
}
