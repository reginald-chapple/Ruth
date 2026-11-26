using Ruth.Modules.Cms.Domain;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.SharedKernel.Metadata;

namespace Ruth.Modules.Cms;

internal static class CmsEntityTypeCatalog
{
    private static readonly EntityTypeRegistration[] _all =
    [
        new(
            CmsSystemGuids.Site,
            CmsSite.EntityTypeKeyName,
            nameof(CmsSite),
            "CMS Site",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CmsSystemGuids.Page,
            CmsPage.EntityTypeKeyName,
            nameof(CmsPage),
            "CMS Page",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CmsSystemGuids.Block,
            CmsBlock.EntityTypeKeyName,
            nameof(CmsBlock),
            "CMS Block",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CmsSystemGuids.ContentChannel,
            ContentChannel.EntityTypeKeyName,
            nameof(ContentChannel),
            "Content Channel",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CmsSystemGuids.ContentChannelItem,
            ContentChannelItem.EntityTypeKeyName,
            nameof(ContentChannelItem),
            "Content Channel Item",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: true
        ),
        new(
            CmsSystemGuids.PersonalizationSegment,
            PersonalizationSegment.EntityTypeKeyName,
            nameof(PersonalizationSegment),
            "Personalization Segment",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CmsSystemGuids.AdaptiveMessage,
            AdaptiveMessage.EntityTypeKeyName,
            nameof(AdaptiveMessage),
            "Adaptive Message",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CmsSystemGuids.MobileApplication,
            MobileApplication.EntityTypeKeyName,
            nameof(MobileApplication),
            "Mobile Application",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
        new(
            CmsSystemGuids.SignageApplication,
            SignageApplication.EntityTypeKeyName,
            nameof(SignageApplication),
            "Signage Application",
            "Cms",
            IsEntity: true,
            IsSecured: true,
            SupportsAttributes: false
        ),
    ];

    public static IReadOnlyList<EntityTypeRegistration> GetAll()
    {
        return _all;
    }
}
