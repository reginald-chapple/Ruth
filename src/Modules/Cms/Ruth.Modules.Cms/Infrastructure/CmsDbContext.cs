using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Cms.Domain.Bookmarks;
using Ruth.Modules.Cms.Domain.Channels;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Modules.Cms.Domain.Datasets;
using Ruth.Modules.Cms.Domain.Engine;
using Ruth.Modules.Cms.Domain.Media;
using Ruth.Modules.Cms.Domain.Personalization;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure;

/// <summary>EF Core database context exclusively owning the cms PostgreSQL schema.</summary>
public sealed class CmsDbContext(DbContextOptions<CmsDbContext> options) : ModuleDbContext(options)
{
    /// <summary>PostgreSQL schema name owned by the Cms module.</summary>
    public const string SchemaName = "cms";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    public DbSet<CmsTheme> Themes => Set<CmsTheme>();
    public DbSet<CmsSite> Sites => Set<CmsSite>();
    public DbSet<SiteDomain> SiteDomains => Set<SiteDomain>();
    public DbSet<CmsLayout> Layouts => Set<CmsLayout>();
    public DbSet<CmsPage> Pages => Set<CmsPage>();
    public DbSet<PageRoute> PageRoutes => Set<PageRoute>();
    public DbSet<PageContext> PageContexts => Set<PageContext>();
    public DbSet<CmsBlockType> BlockTypes => Set<CmsBlockType>();
    public DbSet<CmsBlock> Blocks => Set<CmsBlock>();
    public DbSet<HtmlContent> HtmlContents => Set<HtmlContent>();
    public DbSet<PageShortLink> PageShortLinks => Set<PageShortLink>();
    public DbSet<RequestFilter> RequestFilters => Set<RequestFilter>();

    public DbSet<ContentChannelType> ContentChannelTypes => Set<ContentChannelType>();
    public DbSet<ContentChannel> ContentChannels => Set<ContentChannel>();
    public DbSet<ContentTopic> ContentTopics => Set<ContentTopic>();
    public DbSet<ContentChannelItem> ContentChannelItems => Set<ContentChannelItem>();
    public DbSet<ContentChannelItemAssociation> ContentChannelItemAssociations =>
        Set<ContentChannelItemAssociation>();
    public DbSet<MediaAccount> MediaAccounts => Set<MediaAccount>();
    public DbSet<MediaFolder> MediaFolders => Set<MediaFolder>();
    public DbSet<MediaElement> MediaElements => Set<MediaElement>();
    public DbSet<PersonalLinkSection> PersonalLinkSections => Set<PersonalLinkSection>();
    public DbSet<PersonalLink> PersonalLinks => Set<PersonalLink>();
    public DbSet<PersistedDataset> PersistedDatasets => Set<PersistedDataset>();

    public DbSet<InteractionChannel> InteractionChannels => Set<InteractionChannel>();
    public DbSet<InteractionComponent> InteractionComponents => Set<InteractionComponent>();
    public DbSet<InteractionSession> InteractionSessions => Set<InteractionSession>();
    public DbSet<Interaction> Interactions => Set<Interaction>();
    public DbSet<PersonalizationSegment> PersonalizationSegments => Set<PersonalizationSegment>();
    public DbSet<PersonalizationSegmentMembership> PersonalizationSegmentMemberships =>
        Set<PersonalizationSegmentMembership>();
    public DbSet<AdaptiveMessage> AdaptiveMessages => Set<AdaptiveMessage>();
    public DbSet<AdaptiveMessageAdaptation> AdaptiveMessageAdaptations =>
        Set<AdaptiveMessageAdaptation>();

    public DbSet<MobileApplication> MobileApplications => Set<MobileApplication>();
    public DbSet<MobileLayout> MobileLayouts => Set<MobileLayout>();
    public DbSet<MobilePage> MobilePages => Set<MobilePage>();
    public DbSet<MobileDeepLink> MobileDeepLinks => Set<MobileDeepLink>();
    public DbSet<SignageApplication> SignageApplications => Set<SignageApplication>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.CmsModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
