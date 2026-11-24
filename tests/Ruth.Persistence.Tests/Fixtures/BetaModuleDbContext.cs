using Microsoft.EntityFrameworkCore;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Persistence.Tests.Fixtures;

public sealed class BetaModuleDbContext(DbContextOptions<BetaModuleDbContext> options)
    : ModuleDbContext(options)
{
    protected override string Schema => "beta";

    public DbSet<RowVersionedWidget> BetaWidgets => Set<RowVersionedWidget>();

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.Entity<RowVersionedWidget>(builder =>
        {
            _ = builder.ToTable("beta_widgets");
            _ = builder.HasKey(x => x.Id);
            _ = builder.Property(x => x.Id).ValueGeneratedNever();
            _ = builder.Property(x => x.Label).HasMaxLength(120).IsRequired();
            ConfigurePostgresRowVersion(builder);
        });
    }
}
