using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Events;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Modules.Lms.Contracts.Events;
using Ruth.Modules.Search.Contracts.Collections;
using Ruth.Modules.Search.Contracts.Indexing;
using Ruth.Modules.Search.Contracts.Querying;
using Ruth.Modules.Search.Infrastructure.Indexing;
using Ruth.Modules.Search.Infrastructure.Jobs;
using Ruth.Modules.Search.Infrastructure.Providers;
using Ruth.Modules.Search.Infrastructure.Querying;
using Ruth.Modules.Search.Slices.Collections;
using Ruth.Modules.Search.Slices.Events;
using Ruth.Modules.Search.Slices.Indexing;
using Ruth.Modules.Search.Slices.Querying;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Jobs;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Search.Slices;

internal static class SearchSlicesRegistration
{
    public static void Register(IServiceCollection services)
    {
        RegisterProviders(services);
        RegisterHandlers(services);
    }

    private static void RegisterProviders(IServiceCollection services)
    {
        _ = services.AddScoped<ISearchDocumentProvider, CrmPeopleSearchDocumentProvider>();
        _ = services.AddScoped<ISearchDocumentProvider, GroupsSearchDocumentProvider>();
        _ = services.AddScoped<ISearchDocumentProvider, FinanceAccountsSearchDocumentProvider>();
        _ = services.AddScoped<ISearchDocumentProvider, EventsSearchDocumentProvider>();
        _ = services.AddScoped<ISearchDocumentProvider, CommerceProductsSearchDocumentProvider>();
        _ = services.AddScoped<
            ISearchDocumentProvider,
            EngagementOpportunitiesSearchDocumentProvider
        >();
        _ = services.AddScoped<ISearchDocumentProvider, WorkflowTypesSearchDocumentProvider>();
        _ = services.AddScoped<ISearchDocumentProvider, CmsContentItemsSearchDocumentProvider>();
        _ = services.AddScoped<ISearchDocumentProvider, LmsCoursesSearchDocumentProvider>();
    }

    private static void RegisterHandlers(IServiceCollection services)
    {
        _ = services.AddScoped<SearchDocumentUpsertService>();
        _ = services.AddScoped<PostgreSqlFullTextSearchEngine>();

        _ = services.AddScoped<
            ICommandHandler<CreateContentCollectionCommand, ContentCollectionResponse>,
            CreateContentCollectionCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<GetContentCollectionsQuery, IReadOnlyList<ContentCollectionResponse>>,
            GetContentCollectionsQueryHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<UpsertSearchDocumentsCommand, SearchIndexRebuildResponse>,
            UpsertSearchDocumentsCommandHandler
        >();
        _ = services.AddScoped<
            ICommandHandler<RebuildSearchIndexCommand, SearchIndexRebuildResponse>,
            RebuildSearchIndexCommandHandler
        >();
        _ = services.AddScoped<
            IQueryHandler<ExecuteUniversalSearchQuery, SearchResultsResponse>,
            ExecuteUniversalSearchQueryHandler
        >();

        _ = services.AddScoped<
            IIntegrationEventConsumer<ContentChannelItemPublishedIntegrationEvent>,
            SearchContentChannelItemPublishedConsumer
        >();
        _ = services.AddScoped<
            IIntegrationEventConsumer<LearningCoursePublishedIntegrationEvent>,
            SearchLearningCoursePublishedConsumer
        >();
        _ = services.AddScoped<
            IIntegrationEventConsumer<PersonMergedIntegrationEvent>,
            SearchPersonMergedConsumer
        >();

        _ = services.AddSingleton<IScheduledJob, SearchIndexRebuildJob>();
    }
}
