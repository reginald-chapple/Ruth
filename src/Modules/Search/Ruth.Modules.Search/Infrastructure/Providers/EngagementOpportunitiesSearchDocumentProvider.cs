using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Engagement.Contracts.Pipelines;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Infrastructure.Providers;

internal sealed class EngagementOpportunitiesSearchDocumentProvider(
    IServiceProvider serviceProvider
) : ISearchDocumentProvider
{
    public string ProviderKey => "engagement.opportunities";
    public string ModuleName => "Engagement";
    public string EntityTypeKey => "Ruth.Engagement.EngagementOpportunity";

    public async Task<IReadOnlyList<SearchDocumentUpsertSpec>> LoadDocumentsAsync(
        SearchProviderLoadRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var handler = serviceProvider.GetService<
            IQueryHandler<
                GetEngagementOpportunitiesQuery,
                IReadOnlyList<EngagementOpportunityResponse>
            >
        >();
        if (handler is null)
        {
            return [];
        }

        var res = await handler.HandleAsync(
            new GetEngagementOpportunitiesQuery(ActiveOnly: true),
            cancellationToken
        );
        return res.IsFailure
            ? []
            :
            [
                .. res
                    .Value.Take(Math.Clamp(request.MaxDocuments, 1, 500))
                    .Select(o => new SearchDocumentUpsertSpec(
                        ProviderKey,
                        ModuleName,
                        EntityTypeKey,
                        o.Id,
                        o.Name,
                        Subtitle: o.PipelineName,
                        Summary: o.PublicSummary,
                        BodyText: $"{o.Name} {o.PublicSummary} {o.Description}",
                        Tags: [o.PipelineName, o.Slug],
                        UrlPath: "/engagement/opportunities",
                        IsPublic: o.IsPublic,
                        ContentCollectionId: request.ContentCollectionId
                    )),
            ];
    }
}
