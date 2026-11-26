using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Security;

namespace Ruth.Host.Components.Pages.Search;

public sealed partial class SearchStudio : ComponentBase
{
    private IReadOnlyList<ContentCollectionResponse> _collections = [];
    private SearchResultsResponse? _searchResults;
    private string _collectionName = "Public Member Portal";
    private string _collectionKey = "public-portal";
    private string _searchText = string.Empty;
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
        var res = await QueryDispatcher.QueryAsync<
            GetContentCollectionsQuery,
            IReadOnlyList<ContentCollectionResponse>
        >(new GetContentCollectionsQuery());
        if (res.IsSuccess)
        {
            _collections = res.Value;
        }
    }

    private async Task CreateCollectionAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            CreateContentCollectionCommand,
            ContentCollectionResponse
        >(
            new CreateContentCollectionCommand(
                _collectionName,
                _collectionKey,
                [
                    new ContentCollectionSourceSpec("cms.content-items"),
                    new ContentCollectionSourceSpec("lms.courses"),
                    new ContentCollectionSourceSpec("events.items"),
                    new ContentCollectionSourceSpec("groups.groups"),
                ],
                IsPublic: true
            )
        );
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? $"Created collection '{res.Value.CollectionKey}'."
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }

    private async Task RebuildGlobalIndexAsync()
    {
        var res = await CommandDispatcher.SendAsync<
            RebuildSearchIndexCommand,
            SearchIndexRebuildResponse
        >(new RebuildSearchIndexCommand());
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Indexed {res.Value.DocumentsUpserted} doc(s) across {res.Value.ProvidersExecuted} providers in {res.Value.DurationMilliseconds} ms."
            )
            : res.PrimaryError.Description;
    }

    private async Task ReindexCollectionAsync(Guid collectionId)
    {
        var res = await CommandDispatcher.SendAsync<
            RebuildSearchIndexCommand,
            SearchIndexRebuildResponse
        >(new RebuildSearchIndexCommand(ContentCollectionId: collectionId));
        _isError = res.IsFailure;
        _statusMessage = res.IsSuccess
            ? string.Create(
                CultureInfo.InvariantCulture,
                $"Reindexed {res.Value.DocumentsUpserted} doc(s) for collection."
            )
            : res.PrimaryError.Description;
        if (res.IsSuccess)
        {
            await ReloadAsync();
        }
    }

    private async Task RunSearchAsync()
    {
        var res = await QueryDispatcher.QueryAsync<
            ExecuteUniversalSearchQuery,
            SearchResultsResponse
        >(new ExecuteUniversalSearchQuery(_searchText, SecuritySubject.Anonymous, Limit: 20));
        if (res.IsSuccess)
        {
            _searchResults = res.Value;
        }
    }
}
