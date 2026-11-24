using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Identity.Domain;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Identity.Infrastructure;

/// <summary>EF Core database context for the identity module schema.</summary>
public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>The PostgreSQL schema exclusively owned by the Identity module.</summary>
    public const string SchemaName = "identity";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <summary>Gets the user accounts table.</summary>
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    /// <summary>Gets the security roles table.</summary>
    public DbSet<SecurityRole> SecurityRoles => Set<SecurityRole>();

    /// <summary>Gets the user-to-role join table.</summary>
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    /// <summary>Gets the singleton organization profile table.</summary>
    public DbSet<OrganizationProfile> OrganizationProfiles => Set<OrganizationProfile>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new Configurations.UserAccountConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.SecurityRoleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.UserRoleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.OrganizationProfileConfiguration());
    }
}
