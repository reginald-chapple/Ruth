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

    /// <summary>Gets the rotating refresh tokens table.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <summary>Gets the single-use account action tokens table.</summary>
    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();

    /// <summary>Gets the login history audit table.</summary>
    public DbSet<LoginHistoryEntry> LoginHistoryEntries => Set<LoginHistoryEntry>();

    /// <summary>Gets the external OIDC login links table.</summary>
    public DbSet<ExternalLoginLink> ExternalLoginLinks => Set<ExternalLoginLink>();

    /// <summary>Gets the external OIDC identity provider configurations table.</summary>
    public DbSet<ExternalAuthProviderConfig> ExternalAuthProviders =>
        Set<ExternalAuthProviderConfig>();

    /// <summary>Gets the in-flight remote OIDC authentication sessions table.</summary>
    public DbSet<RemoteAuthenticationSession> RemoteAuthenticationSessions =>
        Set<RemoteAuthenticationSession>();

    /// <summary>Gets the ordered entity ACL rules table.</summary>
    public DbSet<EntityAclRule> EntityAclRules => Set<EntityAclRule>();

    /// <summary>Gets the immutable authorization change audit log table.</summary>
    public DbSet<AuthAuditLog> AuthAuditLogs => Set<AuthAuditLog>();

    /// <summary>Gets the hashed REST API keys table.</summary>
    public DbSet<RestApiKey> RestApiKeys => Set<RestApiKey>();

    /// <summary>Gets the registered OAuth/OIDC clients table.</summary>
    public DbSet<AuthClient> AuthClients => Set<AuthClient>();

    /// <summary>Gets the registered OAuth/OIDC scopes table.</summary>
    public DbSet<AuthScope> AuthScopes => Set<AuthScope>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new Configurations.UserAccountConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.SecurityRoleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.UserRoleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.OrganizationProfileConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.RefreshTokenConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.AccountTokenConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.LoginHistoryEntryConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.ExternalLoginLinkConfiguration());
        _ = modelBuilder.ApplyConfiguration(
            new Configurations.ExternalAuthProviderConfigConfiguration()
        );
        _ = modelBuilder.ApplyConfiguration(
            new Configurations.RemoteAuthenticationSessionConfiguration()
        );
        _ = modelBuilder.ApplyConfiguration(new Configurations.EntityAclRuleConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.AuthAuditLogConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.RestApiKeyConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.AuthClientConfiguration());
        _ = modelBuilder.ApplyConfiguration(new Configurations.AuthScopeConfiguration());
    }
}
