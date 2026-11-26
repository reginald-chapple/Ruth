using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Search.Domain.Collections;
using Ruth.Modules.Search.Domain.Indexing;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Search.Infrastructure;

/// <summary>EF Core database context exclusively owning the search PostgreSQL schema.</summary>
public sealed class SearchDbContext(DbContextOptions<SearchDbContext> options)
    : ModuleDbContext(options)
{
    public const string SchemaName = "search";

    protected override string Schema => SchemaName;

    public DbSet<ContentCollectionDefinition> ContentCollections =>
        Set<ContentCollectionDefinition>();
    public DbSet<ContentCollectionSource> ContentCollectionSources =>
        Set<ContentCollectionSource>();
    public DbSet<SearchDocument> SearchDocuments => Set<SearchDocument>();

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.SearchModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
