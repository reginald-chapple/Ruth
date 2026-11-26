using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal static class CmsContentAndChannelModelRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyContentAndMedia(modelBuilder);
        ApplyPersonalizationAndChannels(modelBuilder);
    }

    private static void ApplyContentAndMedia(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new ContentChannelTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ContentChannelConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ContentTopicConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ContentChannelItemConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ContentChannelItemAssociationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MediaAccountConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MediaFolderConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MediaElementConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonalLinkSectionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonalLinkConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersistedDatasetConfiguration());
    }

    private static void ApplyPersonalizationAndChannels(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new InteractionChannelConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InteractionComponentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InteractionSessionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new InteractionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonalizationSegmentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PersonalizationSegmentMembershipConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AdaptiveMessageConfiguration());
        _ = modelBuilder.ApplyConfiguration(new AdaptiveMessageAdaptationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MobileApplicationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MobileLayoutConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MobilePageConfiguration());
        _ = modelBuilder.ApplyConfiguration(new MobileDeepLinkConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SignageApplicationConfiguration());
    }
}
