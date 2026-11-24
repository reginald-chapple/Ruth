using Microsoft.EntityFrameworkCore;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed class AlphaModuleDbContext(DbContextOptions<AlphaModuleDbContext> options)
    : ModuleDbContext(options)
{
    protected override string Schema => "alpha";

    public DbSet<AuditedWidget> AuditedWidgets => Set<AuditedWidget>();

    public DbSet<RowVersionedWidget> RowVersionedWidgets => Set<RowVersionedWidget>();

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<AuditedWidget>(builder =>
        {
            _ = builder.ToTable("audited_widgets");
            _ = builder.HasKey(x => x.Id);
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
            _ = builder.Property(x => x.EffectiveAt).IsRequired();
            _ = builder.Property(x => x.CreatedOnUtc).IsRequired();
            _ = builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken().IsRequired();
            _ = builder.Ignore(x => x.DomainEvents);
            _ = builder.Ignore(x => x.IntegrationEvents);
        });

        _ = modelBuilder.Entity<RowVersionedWidget>(builder =>
        {
            _ = builder.ToTable("row_versioned_widgets");
            _ = builder.HasKey(x => x.Id);
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
            ConfigurePostgresRowVersion(builder);
        });
    }
}
