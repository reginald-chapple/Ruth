using Ruth.Modules.Cms.Contracts.Engine;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Slices;

internal static class CmsProjectionMapper
{
    public static CmsThemeResponse ToResponse(CmsTheme entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.CssCustomProperties,
            entity.CustomCss,
            entity.RenderCssBundle(),
            entity.IsActive,
            entity.IsSystem
        );
    }

    public static CmsSiteResponse ToResponse(CmsSite entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.ThemeId?.Value,
            entity.DefaultPageId?.Value,
            entity.LoginPageId?.Value,
            entity.NotFoundPageId?.Value,
            entity.BranchId,
            entity.SiteType,
            entity.EnablePageViews,
            entity.IsActive,
            [
                .. entity
                    .Domains.OrderBy(static d => d.Order)
                    .Select(static d => new SiteDomainResponse(
                        d.Id.Value,
                        d.SiteId.Value,
                        d.Domain,
                        d.Order,
                        d.IsPrimary
                    )),
            ],
            entity.AttributeValues
        );
    }

    public static CmsLayoutResponse ToResponse(CmsLayout entity)
    {
        return new(
            entity.Id.Value,
            entity.SiteId.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.FileName,
            entity.Zones,
            entity.IsActive
        );
    }

    public static CmsPageResponse ToResponse(CmsPage entity)
    {
        return new(
            entity.Id.Value,
            entity.SiteId.Value,
            entity.LayoutId.Value,
            entity.ParentPageId?.Value,
            entity.InternalName,
            entity.PageTitle,
            entity.BrowserTitle,
            entity.Description,
            entity.IconCssClass,
            entity.Order,
            entity.DisplayInNavWhen,
            entity.RequiresEncryption,
            entity.OutputCacheDurationSeconds,
            entity.IsPublished,
            [
                .. entity.Routes.Select(static r => new PageRouteResponse(
                    r.Id.Value,
                    r.PageId.Value,
                    r.Route,
                    r.IsGlobal
                )),
            ],
            [
                .. entity.Contexts.Select(static c => new PageContextResponse(
                    c.Id.Value,
                    c.PageId.Value,
                    c.EntityKey,
                    c.IdParameter
                )),
            ],
            entity.AttributeValues
        );
    }

    public static CmsBlockTypeResponse ToResponse(CmsBlockType entity)
    {
        return new(
            entity.Id.Value,
            entity.Key,
            entity.Name,
            entity.Category,
            entity.Description,
            entity.RequiresInteractivity,
            entity.DefaultSettings,
            entity.IsSystem
        );
    }

    public static CmsBlockResponse ToResponse(CmsBlock entity, CmsBlockType blockType)
    {
        return new(
            entity.Id.Value,
            entity.BlockTypeId.Value,
            blockType.Key,
            blockType.Name,
            blockType.RequiresInteractivity,
            entity.PageId?.Value,
            entity.LayoutId?.Value,
            entity.SiteId?.Value,
            entity.Zone,
            entity.Name,
            entity.Order,
            entity.PreHtml,
            entity.PostHtml,
            entity.CssClass,
            entity.OutputCacheDurationSeconds,
            entity.IsActive,
            entity.Settings,
            entity.AttributeValues
        );
    }

    public static HtmlContentResponse ToResponse(HtmlContent entity)
    {
        return new(
            entity.Id.Value,
            entity.BlockId.Value,
            entity.EntityValue,
            entity.Version,
            entity.Content,
            entity.ContentSummary,
            entity.StartDateTimeUtc,
            entity.ExpireDateTimeUtc,
            entity.IsApproved,
            entity.ApprovedByPersonId,
            entity.ApprovedDateTimeUtc,
            entity.CreatedOnUtc
        );
    }

    public static PageShortLinkResponse ToResponse(PageShortLink entity)
    {
        return new(
            entity.Id.Value,
            entity.SiteId.Value,
            entity.Token,
            entity.BuildResolvedTargetUrl(),
            entity.CategoryId,
            entity.IsPinned,
            entity.ClickCount,
            entity.UtmSource,
            entity.UtmMedium,
            entity.UtmCampaign,
            entity.LastClickedUtc
        );
    }

    public static RequestFilterResponse ToResponse(RequestFilter entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Key,
            entity.SiteId?.Value,
            entity.FilterMode,
            entity.Rules,
            entity.IsActive
        );
    }
}
