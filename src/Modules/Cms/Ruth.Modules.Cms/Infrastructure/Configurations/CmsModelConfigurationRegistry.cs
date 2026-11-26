using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal static class CmsModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        ApplyEngineConfigurations(modelBuilder);
        CmsContentAndChannelModelRegistry.ApplyAll(modelBuilder);
    }

    private static void ApplyEngineConfigurations(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new CmsThemeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CmsSiteConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SiteDomainConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CmsLayoutConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CmsPageConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PageRouteConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PageContextConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CmsBlockTypeConfiguration());
        _ = modelBuilder.ApplyConfiguration(new CmsBlockConfiguration());
        _ = modelBuilder.ApplyConfiguration(new HtmlContentConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PageShortLinkConfiguration());
        _ = modelBuilder.ApplyConfiguration(new RequestFilterConfiguration());
    }
}
