using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Tenancy.Domain;
using Ruth.Modules.Tenancy.Infrastructure.Configurations;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Tenancy.Infrastructure;

/// <summary>Platform control-plane EF Core <see cref="ModuleDbContext" /> owning the platform schema.</summary>
public sealed class TenancyDbContext(DbContextOptions<TenancyDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>Gets the PostgreSQL schema name for platform tenancy tables.</summary>
    public const string SchemaName = "platform";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    /// <inheritdoc />
    protected override bool IsTenantScopedSchema => false;

    /// <summary>Gets the registered organization tenants table.</summary>
    public DbSet<Tenant> Tenants => Set<Tenant>();

    /// <summary>Gets the sandbox and custom domains table.</summary>
    public DbSet<TenantDomain> TenantDomains => Set<TenantDomain>();

    /// <summary>Gets the IRS EO BMF verification audit records table.</summary>
    public DbSet<IrsExemptVerification> IrsExemptVerifications => Set<IrsExemptVerification>();

    /// <summary>Gets the Stripe Connect Express onboarding accounts table.</summary>
    public DbSet<StripeConnectAccount> StripeConnectAccounts => Set<StripeConnectAccount>();

    /// <summary>Gets the internal platform operators table.</summary>
    public DbSet<PlatformOperator> PlatformOperators => Set<PlatformOperator>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        _ = modelBuilder.ApplyConfiguration(new TenantConfiguration());
        _ = modelBuilder.ApplyConfiguration(new TenantDomainConfiguration());
        _ = modelBuilder.ApplyConfiguration(new IrsExemptVerificationConfiguration());
        _ = modelBuilder.ApplyConfiguration(new StripeConnectAccountConfiguration());
        _ = modelBuilder.ApplyConfiguration(new PlatformOperatorConfiguration());
    }
}
