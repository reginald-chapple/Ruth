using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Infrastructure.Datasets;
using Ruth.Modules.Cms.Infrastructure.Media;
using Ruth.Modules.Cms.Slices.Bookmarks;
using Ruth.Modules.Cms.Slices.Content;
using Ruth.Modules.Cms.Slices.Datasets;
using Ruth.Modules.Cms.Slices.Media;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S2CmsContentRegistration
{
    public static void Register(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMediaAccountSyncProvider, InMemoryMediaSyncProvider>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMediaAccountSyncProvider, VimeoMediaSyncProvider>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMediaAccountSyncProvider, YouTubeMediaSyncProvider>()
        );
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IMediaAccountSyncProvider, HlsMediaSyncProvider>()
        );
        services.TryAddScoped<PersistedDatasetMaterializer>();
        _ = services.AddScheduledJob<PersistedDatasetRefreshJob>();

        RegisterContentHandlers(services);
        RegisterMediaHandlers(services);
        RegisterBookmarkAndDatasetHandlers(services);
    }

    private static void RegisterContentHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateContentChannelTypeCommand,
            ContentChannelTypeResponse,
            CreateContentChannelTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetContentChannelTypesQuery,
            IReadOnlyList<ContentChannelTypeResponse>,
            GetContentChannelTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateContentChannelCommand,
            ContentChannelResponse,
            CreateContentChannelCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetContentChannelsQuery,
            IReadOnlyList<ContentChannelResponse>,
            GetContentChannelsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateContentTopicCommand,
            ContentTopicResponse,
            CreateContentTopicCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetContentTopicsQuery,
            IReadOnlyList<ContentTopicResponse>,
            GetContentTopicsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateContentChannelItemCommand,
            ContentChannelItemResponse,
            CreateContentChannelItemCommandHandler
        >();
        _ = services.AddCommandHandler<
            ModerateContentChannelItemCommand,
            ContentChannelItemResponse,
            ModerateContentChannelItemCommandHandler
        >();
        _ = services.AddCommandHandler<
            AssociateContentChannelItemsCommand,
            ContentChannelItemResponse,
            AssociateContentChannelItemsCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetContentChannelItemsQuery,
            IReadOnlyList<ContentChannelItemResponse>,
            GetContentChannelItemsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetContentChannelFeedQuery,
            ContentChannelFeedResponse,
            GetContentChannelFeedQueryHandler
        >();
    }

    private static void RegisterMediaHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateMediaAccountCommand,
            MediaAccountResponse,
            CreateMediaAccountCommandHandler
        >();
        _ = services.AddCommandHandler<
            SyncMediaAccountCommand,
            MediaAccountResponse,
            SyncMediaAccountCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetMediaAccountsQuery,
            IReadOnlyList<MediaAccountResponse>,
            GetMediaAccountsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetMediaFoldersQuery,
            IReadOnlyList<MediaFolderResponse>,
            GetMediaFoldersQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetMediaElementsQuery,
            IReadOnlyList<MediaElementResponse>,
            GetMediaElementsQueryHandler
        >();
    }

    private static void RegisterBookmarkAndDatasetHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreatePersonalLinkSectionCommand,
            PersonalLinkSectionResponse,
            CreatePersonalLinkSectionCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddPersonalLinkCommand,
            PersonalLinkSectionResponse,
            AddPersonalLinkCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersonalLinkSectionsQuery,
            IReadOnlyList<PersonalLinkSectionResponse>,
            GetPersonalLinkSectionsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreatePersistedDatasetCommand,
            PersistedDatasetResponse,
            CreatePersistedDatasetCommandHandler
        >();
        _ = services.AddCommandHandler<
            RefreshPersistedDatasetCommand,
            PersistedDatasetResponse,
            RefreshPersistedDatasetCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPersistedDatasetsQuery,
            IReadOnlyList<PersistedDatasetResponse>,
            GetPersistedDatasetsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetPersistedDatasetByKeyQuery,
            PersistedDatasetResponse,
            GetPersistedDatasetByKeyQueryHandler
        >();
    }
}
