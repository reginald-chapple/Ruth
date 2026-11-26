using Ruth.Modules.Cms.Contracts.Bookmarks;
using Ruth.Modules.Cms.Contracts.Content;
using Ruth.Modules.Cms.Contracts.Datasets;
using Ruth.Modules.Cms.Contracts.Media;
using Ruth.Modules.Cms.Domain.Bookmarks;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Domain.Datasets;
using Ruth.Modules.Cms.Domain.Media;

namespace Ruth.Modules.Cms.Slices;

internal static class CmsContentProjectionMapper
{
    public static ContentChannelTypeResponse ToResponse(ContentChannelType entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Key,
            entity.DateRangeType,
            entity.DisablePriority,
            entity.DisableStatus,
            entity.IncludeTime,
            entity.IsSystem
        );
    }

    public static ContentChannelResponse ToResponse(ContentChannel entity)
    {
        return new(
            entity.Id.Value,
            entity.ContentChannelTypeId.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.IconCssClass,
            entity.RequiresApproval,
            entity.EnableRss,
            entity.ChannelUrl,
            entity.ItemUrl,
            entity.TimeToLiveMinutes,
            entity.ItemsManuallyOrdered,
            entity.IsStructuredContent,
            entity.IsActive,
            entity.ChildChannelIds,
            entity.AttributeValues
        );
    }

    public static ContentTopicResponse ToResponse(ContentTopic entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.Slug,
            entity.Description,
            entity.ParentTopicId?.Value,
            entity.Order,
            entity.IsActive
        );
    }

    public static ContentChannelItemResponse ToResponse(ContentChannelItem entity)
    {
        return new(
            entity.Id.Value,
            entity.ContentChannelId.Value,
            entity.Title,
            entity.Slug,
            entity.Summary,
            entity.Content,
            entity.Priority,
            entity.Status,
            entity.ApprovedByPersonId,
            entity.ApprovedDateTimeUtc,
            entity.StartDateTimeUtc,
            entity.ExpireDateTimeUtc,
            entity.Order,
            entity.AuthorPersonId,
            entity.PrimaryTopicId?.Value,
            entity.TopicIds,
            [
                .. entity
                    .ChildAssociations.OrderBy(static a => a.Order)
                    .Select(static a => a.ChildItemId.Value),
            ],
            entity.AttributeValues
        );
    }

    public static MediaElementResponse ToResponse(MediaElement entity)
    {
        return new(
            entity.Id.Value,
            entity.MediaFolderId.Value,
            entity.Name,
            entity.ExternalElementKey,
            entity.Description,
            entity.DurationSeconds,
            entity.ThumbnailUrl,
            entity.PlaybackUrl,
            entity.HlsManifestUrl,
            entity.CaptionsUrl,
            entity.Width,
            entity.Height,
            entity.PublishedDateTimeUtc,
            entity.Metadata
        );
    }

    public static PersonalLinkSectionResponse ToResponse(PersonalLinkSection entity)
    {
        return new(
            entity.Id.Value,
            entity.Name,
            entity.PersonId,
            entity.IsShared,
            entity.Order,
            [
                .. entity
                    .Links.OrderBy(static l => l.Order)
                    .Select(static l => new PersonalLinkResponse(
                        l.Id.Value,
                        l.SectionId.Value,
                        l.Name,
                        l.Url,
                        l.IconName,
                        l.Order
                    )),
            ]
        );
    }

    public static PersistedDatasetResponse ToResponse(PersistedDataset entity)
    {
        return new(
            entity.Id.Value,
            entity.AccessKey,
            entity.Name,
            entity.Description,
            entity.DataSourceKind,
            entity.SourceConfiguration,
            entity.RefreshIntervalMinutes,
            entity.ExpireDateTimeUtc,
            entity.MemoryCacheDurationMinutes,
            entity.ResultDataJson,
            entity.LastRefreshDateTimeUtc,
            entity.TimeToBuildMs,
            entity.IsActive,
            entity.AllowManualRefresh
        );
    }
}
