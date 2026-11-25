using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Domain.Templates;
using Ruth.Modules.Communication.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Communication.Infrastructure;

/// <summary>EF Core database context exclusively owning the comms PostgreSQL schema.</summary>
public sealed class CommunicationDbContext(DbContextOptions<CommunicationDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the Communication module.</summary>
    public const string SchemaName = "comms";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the reusable template snippets table.</summary>
    public DbSet<TemplateSnippet> TemplateSnippets => Set<TemplateSnippet>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new TemplateSnippetConfiguration());
    }
}
