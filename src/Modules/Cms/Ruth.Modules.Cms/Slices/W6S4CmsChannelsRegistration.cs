using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Slices.Channels;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S4CmsChannelsRegistration
{
    public static void Register(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateMobileApplicationCommand,
            MobileApplicationResponse,
            CreateMobileApplicationCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddMobileLayoutCommand,
            MobileApplicationResponse,
            AddMobileLayoutCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddMobilePageCommand,
            MobileApplicationResponse,
            AddMobilePageCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddMobileDeepLinkCommand,
            MobileApplicationResponse,
            AddMobileDeepLinkCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetMobileApplicationsQuery,
            IReadOnlyList<MobileApplicationResponse>,
            GetMobileApplicationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetMobileAppBootstrapQuery,
            MobileAppBootstrapResponse,
            GetMobileAppBootstrapQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetHydratedMobilePageQuery,
            HydratedMobilePageResponse,
            GetHydratedMobilePageQueryHandler
        >();
        _ = services.AddQueryHandler<
            ResolveMobileDeepLinkQuery,
            ResolvedMobileDeepLinkResponse,
            ResolveMobileDeepLinkQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateSignageApplicationCommand,
            SignageApplicationResponse,
            CreateSignageApplicationCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetSignageApplicationsQuery,
            IReadOnlyList<SignageApplicationResponse>,
            GetSignageApplicationsQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetSignageFeedQuery,
            SignageFeedResponse,
            GetSignageFeedQueryHandler
        >();
    }
}
