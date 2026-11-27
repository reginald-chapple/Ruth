using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Ruth.Persistence.DependencyInjection;
using Ruth.Persistence.EntityFramework;
using Ruth.Persistence.Tests.Fixtures;
using Ruth.SharedKernel.Tenancy;
using Xunit;

namespace Ruth.Persistence.Tests.EntityFramework;

[Collection("Postgres18")]
public sealed class TenantModelConventionTests(Postgres18ContainerFixture fixture)
{
    private readonly Postgres18ContainerFixture _fixture = fixture;

    private sealed class UnscopedRogueEntity
    {
        public Guid Id { get; set; }
    }

    private sealed class RogueTenantDbContext(DbContextOptions<RogueTenantDbContext> options)
        : ModuleDbContext(options)
    {
        protected override string Schema => "rogue";

        public DbSet<UnscopedRogueEntity> RogueItems => Set<UnscopedRogueEntity>();
    }

    private sealed class NullsNotDistinctItem : ITenantScoped
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public string? OptionalScope { get; set; }

        public TenantId TenantId { get; private set; }

        public void AssignTenant(TenantId tenantId)
        {
            TenantId = tenantId;
        }
    }

    private sealed class IndexedTenantDbContext(DbContextOptions<IndexedTenantDbContext> options)
        : ModuleDbContext(options)
    {
        protected override string Schema => "idx_test";

        public DbSet<NullsNotDistinctItem> Items => Set<NullsNotDistinctItem>();

        protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<NullsNotDistinctItem>(b =>
            {
                _ = b.ToTable("items");
                _ = b.HasKey(x => x.Id);
                _ = b.Property(x => x.Id).ValueGeneratedNever();
                _ = b.Property(x => x.Code).HasMaxLength(64).IsRequired();
                _ = b.Property(x => x.OptionalScope).HasMaxLength(64);
                _ = b.HasIndex(x => new { x.Code, x.OptionalScope })
                    .IsUnique()
                    .AreNullsDistinct(nullsDistinct: false);
            });
        }
    }

    [Fact]
    public void Non_ITenantScoped_Entity_In_Tenant_DbContext_Fails_Model_Creation()
    {
        var opts = new DbContextOptionsBuilder<RogueTenantDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=u;Password=p")
            .Options;
        using var ctx = new RogueTenantDbContext(opts);
        _ = Assert.Throws<InvalidOperationException>(() => _ = ctx.Model);
    }

    [Fact]
    public void Owned_And_Derived_Entities_Enforce_SameTable_Rls_Conventions()
    {
        var validOpts = new DbContextOptionsBuilder<ValidOwnedAndTphDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=u;Password=p")
            .Options;
        using (var validCtx = new ValidOwnedAndTphDbContext(validOpts))
        {
            Assert.NotNull(validCtx.Model);
        }

        var separateOwnedOpts = new DbContextOptionsBuilder<SeparateOwnedTableDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=u;Password=p")
            .Options;
        using (var badOwnedCtx = new SeparateOwnedTableDbContext(separateOwnedOpts))
        {
            _ = Assert.Throws<InvalidOperationException>(() => _ = badOwnedCtx.Model);
        }

        var tptOpts = new DbContextOptionsBuilder<TptDerivedDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=u;Password=p")
            .Options;
        using var badTptCtx = new TptDerivedDbContext(tptOpts);
        _ = Assert.Throws<InvalidOperationException>(() => _ = badTptCtx.Model);
    }

    [Fact]
    public async Task ScopeUniqueIndexesToTenant_Preserves_NullsNotDistinct_Per_Tenant()
    {
        var connStr = await _fixture.CreateIsolatedDatabaseAsync();
        var tenantA = TenantId.Create();
        var tenantB = TenantId.Create();

        var services = new ServiceCollection();
        _ = services.AddLogging();
        _ = services.AddRuthPersistence(connStr);
        _ = services.AddModuleDbContext<IndexedTenantDbContext>(connStr, "idx_test");
        await using var provider = services.BuildServiceProvider();

        using (AmbientTenantContext.BeginScope(tenantA))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IndexedTenantDbContext>();
            await TestSchemaHelper.EnsureSchemaTablesCreatedAsync(db, "idx_test");

            _ = db.Items.Add(
                new NullsNotDistinctItem
                {
                    Id = Guid.CreateVersion7(),
                    Code = "GEN",
                    OptionalScope = null,
                }
            );
            _ = await db.SaveChangesAsync();

            _ = db.Items.Add(
                new NullsNotDistinctItem
                {
                    Id = Guid.CreateVersion7(),
                    Code = "GEN",
                    OptionalScope = null,
                }
            );
            _ = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        using (AmbientTenantContext.BeginScope(tenantB))
        {
            await using var scope = provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<IndexedTenantDbContext>();
            _ = db.Items.Add(
                new NullsNotDistinctItem
                {
                    Id = Guid.CreateVersion7(),
                    Code = "GEN",
                    OptionalScope = null,
                }
            );
            _ = await db.SaveChangesAsync();
        }
    }

    private sealed class OwnedAddress
    {
        public string Street { get; set; } = string.Empty;
    }

    private abstract class TphBaseItem : ITenantScoped
    {
        public Guid Id { get; set; }

        public string Code { get; set; } = string.Empty;

        public TenantId TenantId { get; private set; }

        public void AssignTenant(TenantId tenantId)
        {
            TenantId = tenantId;
        }
    }

    private sealed class TphSpecialItem : TphBaseItem
    {
        public string SpecialKey { get; set; } = string.Empty;

        public OwnedAddress Address { get; set; } = new();
    }

    private sealed class ValidOwnedAndTphDbContext(
        DbContextOptions<ValidOwnedAndTphDbContext> options
    ) : ModuleDbContext(options)
    {
        protected override string Schema => "tph_ok";

        public DbSet<TphBaseItem> BaseItems => Set<TphBaseItem>();

        protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<TphBaseItem>(b =>
            {
                _ = b.ToTable("base_items");
                _ = b.HasKey(x => x.Id);
            });
            _ = modelBuilder.Entity<TphSpecialItem>(b =>
            {
                _ = b.HasIndex(x => x.SpecialKey)
                    .IsUnique()
                    .HasDatabaseName("IX_custom_special")
                    .HasFilter("\"SpecialKey\" IS NOT NULL")
                    .IncludeProperties(x => x.Code)
                    .IsDescending(true);
                _ = b.OwnsOne(x => x.Address);
            });
        }
    }

    private sealed class SeparateOwnedTableDbContext(
        DbContextOptions<SeparateOwnedTableDbContext> options
    ) : ModuleDbContext(options)
    {
        protected override string Schema => "owned_bad";

        public DbSet<TphSpecialItem> Items => Set<TphSpecialItem>();

        protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<TphSpecialItem>(b =>
            {
                _ = b.ToTable("special_items");
                _ = b.HasKey(x => x.Id);
                _ = b.OwnsOne(x => x.Address, ob => _ = ob.ToTable("separate_addresses"));
            });
        }
    }

    private sealed class TptDerivedDbContext(DbContextOptions<TptDerivedDbContext> options)
        : ModuleDbContext(options)
    {
        protected override string Schema => "tpt_bad";

        public DbSet<TphBaseItem> BaseItems => Set<TphBaseItem>();

        protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<TphBaseItem>(b =>
            {
                _ = b.ToTable("base_items");
                _ = b.HasKey(x => x.Id);
            });
            _ = modelBuilder.Entity<TphSpecialItem>(b =>
            {
                _ = b.ToTable("tpt_special_items");
                _ = b.OwnsOne(x => x.Address);
            });
        }
    }
}
