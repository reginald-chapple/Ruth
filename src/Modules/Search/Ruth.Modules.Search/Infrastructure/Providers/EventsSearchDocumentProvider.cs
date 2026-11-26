using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class EventsSearchDocumentProvider(IServiceProvider serviceProvider)
    : ISearchDocumentProvider
{
    public string ProviderKey => "events.items";
    public string ModuleName => "Events";
    public string EntityTypeKey => "Ruth.Events.EventItem";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<GetEventItemsQuery, IReadOnlyList<EventItemResponse>>
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetEventItemsQuery(ActiveOnly: true),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(e => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        e.Id,
                        e.Name,
                        Subtitle: e.Slug,
                        Summary: e.Summary,
                        BodyText: $"{e.Name} {e.Summary} {e.Description}",
                        Tags: [e.Slug, e.ApprovalStatus.ToString(), "Event"],
                        UrlPath: "/events/calendars",
                        IsPublic: e.IsPublic,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
