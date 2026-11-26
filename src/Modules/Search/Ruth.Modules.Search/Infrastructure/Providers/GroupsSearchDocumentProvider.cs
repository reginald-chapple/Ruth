using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Groups.Contracts.Groups;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class GroupsSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "groups.groups";
    public string ModuleName => "Groups";
    public string EntityTypeKey => "Ruth.Groups.Group";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<FindGroupsQuery, IReadOnlyList<GroupFinderItemResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new FindGroupsQuery(
                PublicOnly: false,
                ActiveOnly: true,
                MaxResults: Math.Clamp(request.MaxDocuments, 1, 500)
            ),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res.Value.Select(g => new SearchDocumentUpsertSpec(
                    ProviderKey,
                    ModuleName,
                    EntityTypeKey,
                    g.GroupId,
                    g.Name,
                    Subtitle: g.GroupTypeName,
                    Summary: g.Description,
                    BodyText: $"{g.Name} {g.GroupTypeName} {g.LocationName} {g.Description}",
                    Tags: [g.GroupTypeName, g.MeetingStyle?.ToString() ?? "Group"],
                    UrlPath: $"/groups?selected={g.GroupId:D}",
                    IsPublic: true,
                    ContentCollectionId: request.ContentCollectionId
                )),
            ];
    }
}
