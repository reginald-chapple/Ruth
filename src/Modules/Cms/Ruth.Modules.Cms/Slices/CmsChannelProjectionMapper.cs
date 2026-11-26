using Ruth.Modules.Cms.Contracts.Channels;
using Ruth.Modules.Cms.Contracts.Personalization;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Slices;

internal static class CmsChannelProjectionMapper
{
    public static PersonalizationSegmentResponse ToResponse(PersonalizationSegment entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.SegmentKey,
            entity.Description,
            entity.TimeToUpdateIntervalMinutes,
            entity.IsActive,
            entity.IsDirty,
            entity.FilterSpec,
            entity.LastEvaluatedUtc,
            entity.CachedMemberCount
        );
    }

    public static AdaptiveMessageAdaptationResponse ToResponse(AdaptiveMessageAdaptation entity)
    {
        return new(
            entity.Id.Value,
            entity.AdaptiveMessageId.Value,
            entity.Name,
            entity.Order,
            entity.SegmentKeys,
            entity.StartDateTimeUtc,
            entity.ExpireDateTimeUtc,
            entity.ViewSaturationCount,
            entity.ViewSaturationInDays,
            entity.Headline,
            entity.BodyMarkup,
            entity.CallToActionText,
            entity.CallToActionUrl,
            entity.ImageUrl,
            entity.IsActive
        );
    }

    public static AdaptiveMessageResponse ToResponse(AdaptiveMessage entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Key,
            entity.Description,
            entity.CategoryId,
            entity.StartDateTimeUtc,
            entity.ExpireDateTimeUtc,
            entity.IsActive,
            [.. entity.Adaptations.OrderBy(static a => a.Order).Select(ToResponse)]
        );
    }

    public static MobileLayoutResponse ToResponse(MobileLayout entity)
    {
        return new(
            entity.Id.Value,
            entity.MobileApplicationId.Value,
            entity.Name,
            entity.LayoutKey,
            entity.ShellMarkup,
            entity.Order
        );
    }

    public static MobilePageResponse ToResponse(MobilePage entity)
    {
        return new(
            entity.Id.Value,
            entity.MobileApplicationId.Value,
            entity.MobileLayoutId?.Value,
            entity.Title,
            entity.PageKey,
            entity.Route,
            entity.IconName,
            entity.Order,
            entity.ShowInNavigation,
            entity.RequiresAuthentication,
            entity.ContentChannelId?.Value,
            entity.AdaptiveMessageKey,
            entity.BlocksJson,
            entity.IsActive
        );
    }

    public static MobileDeepLinkResponse ToResponse(MobileDeepLink entity)
    {
        return new(
            entity.Id.Value,
            entity.MobileApplicationId.Value,
            entity.RoutePrefix,
            entity.TargetMobilePageId?.Value,
            entity.FallbackUrl,
            entity.UsesUniversalLinks,
            entity.IsActive
        );
    }

    public static MobileApplicationResponse ToResponse(MobileApplication entity)
    {
        return new(
            entity.Id.Value,
            entity.SiteId?.Value,
            entity.Name,
            entity.Slug,
            entity.BundleIdentifier,
            entity.ApiVersion,
            entity.MinimumClientVersion,
            entity.LatestClientVersion,
            entity.NavigationType,
            entity.PrimaryColorHex,
            entity.SecondaryColorHex,
            entity.AdditionalSettings,
            entity.IsActive,
            [.. entity.Layouts.OrderBy(static l => l.Order).Select(ToResponse)],
            [.. entity.Pages.OrderBy(static p => p.Order).Select(ToResponse)],
            [.. entity.DeepLinks.Select(ToResponse)]
        );
    }

    public static SignageApplicationResponse ToResponse(SignageApplication entity)
    {
        return new(
            entity.Id.Value,
            entity.SiteId?.Value,
            entity.Name,
            entity.Slug,
            entity.Platform,
            entity.RefreshIntervalSeconds,
            entity.ThemeId?.Value,
            entity.DefaultContentChannelId?.Value,
            entity.MediaFolderId?.Value,
            entity.AdaptiveMessageKey,
            entity.Slides,
            entity.IsActive
        );
    }
}
