using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.Search.Infrastructure.Configurations;

internal static class SearchModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new ContentCollectionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new ContentCollectionSourceConfiguration());
        _ = modelBuilder.ApplyConfiguration(new SearchDocumentConfiguration());
    }
}
