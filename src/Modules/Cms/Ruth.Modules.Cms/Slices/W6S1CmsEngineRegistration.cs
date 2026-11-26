using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Slices.Engine;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Cms.Slices;

internal static class W6S1CmsEngineRegistration
{
    public static void Register(IServiceCollection services)
    {
        _ = services.AddScoped<CmsPageBlocksLoader>();
        RegisterSiteAndPageHandlers(services);
        RegisterBlockHandlers(services);
        RegisterShortLinkAndFilterHandlers(services);
    }

    private static void RegisterSiteAndPageHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateCmsThemeCommand,
            CmsThemeResponse,
            CreateCmsThemeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCmsThemesQuery,
            IReadOnlyList<CmsThemeResponse>,
            GetCmsThemesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateCmsSiteCommand,
            CmsSiteResponse,
            CreateCmsSiteCommandHandler
        >();
        _ = services.AddCommandHandler<
            AddSiteDomainCommand,
            CmsSiteResponse,
            AddSiteDomainCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCmsSitesQuery,
            IReadOnlyList<CmsSiteResponse>,
            GetCmsSitesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateCmsLayoutCommand,
            CmsLayoutResponse,
            CreateCmsLayoutCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCmsLayoutsQuery,
            IReadOnlyList<CmsLayoutResponse>,
            GetCmsLayoutsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateCmsPageCommand,
            CmsPageResponse,
            CreateCmsPageCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCmsPagesQuery,
            IReadOnlyList<CmsPageResponse>,
            GetCmsPagesQueryHandler
        >();
    }

    private static void RegisterBlockHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            RegisterCmsBlockTypeCommand,
            CmsBlockTypeResponse,
            RegisterCmsBlockTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCmsBlockTypesQuery,
            IReadOnlyList<CmsBlockTypeResponse>,
            GetCmsBlockTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateCmsBlockCommand,
            CmsBlockResponse,
            CreateCmsBlockCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCmsBlockCommand,
            CmsBlockResponse,
            UpdateCmsBlockCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetCmsBlocksQuery,
            IReadOnlyList<CmsBlockResponse>,
            GetCmsBlocksQueryHandler
        >();
        _ = services.AddCommandHandler<
            SaveHtmlContentVersionCommand,
            HtmlContentResponse,
            SaveHtmlContentVersionCommandHandler
        >();
        _ = services.AddCommandHandler<
            ApproveHtmlContentCommand,
            HtmlContentResponse,
            ApproveHtmlContentCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetHtmlContentVersionsQuery,
            IReadOnlyList<HtmlContentResponse>,
            GetHtmlContentVersionsQueryHandler
        >();
    }

    private static void RegisterShortLinkAndFilterHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            ResolveCmsPageQuery,
            ResolvedCmsPageResponse,
            ResolveCmsPageQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreatePageShortLinkCommand,
            PageShortLinkResponse,
            CreatePageShortLinkCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPageShortLinksQuery,
            IReadOnlyList<PageShortLinkResponse>,
            GetPageShortLinksQueryHandler
        >();
        _ = services.AddCommandHandler<
            ResolvePageShortLinkCommand,
            PageShortLinkResponse,
            ResolvePageShortLinkCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetShortLinkQrQuery,
            ShortLinkQrResponse,
            GetShortLinkQrQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateRequestFilterCommand,
            RequestFilterResponse,
            CreateRequestFilterCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRequestFiltersQuery,
            IReadOnlyList<RequestFilterResponse>,
            GetRequestFiltersQueryHandler
        >();
        _ = services.AddQueryHandler<
            EvaluateRequestFilterQuery,
            RequestFilterEvaluationResponse,
            EvaluateRequestFilterQueryHandler
        >();
    }
}
