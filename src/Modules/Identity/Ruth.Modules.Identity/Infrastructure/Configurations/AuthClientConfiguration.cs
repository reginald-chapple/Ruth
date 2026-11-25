using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class AuthClientConfiguration : IEntityTypeConfiguration<AuthClient>
{
    public void Configure(EntityTypeBuilder<AuthClient> builder)
    {
        _ = builder.ToTable("auth_clients");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        _ = builder
            .Property(x => x.ClientId)
            .HasColumnName("client_id")
            .HasMaxLength(50)
            .IsRequired();
        _ = builder.HasIndex(x => x.ClientId).IsUnique();
        _ = builder
            .Property(x => x.ClientSecretHash)
            .HasColumnName("client_secret_hash")
            .HasMaxLength(256);
        _ = builder
            .Property(x => x.RedirectUri)
            .HasColumnName("redirect_uri")
            .HasMaxLength(500)
            .IsRequired();
        _ = builder
            .Property(x => x.PostLogoutRedirectUri)
            .HasColumnName("post_logout_redirect_uri")
            .HasMaxLength(500)
            .IsRequired();
        _ = builder
            .Property(x => x.AllowedScopes)
            .HasColumnName("allowed_scopes")
            .HasMaxLength(500)
            .IsRequired();
        _ = builder
            .Property(x => x.AllowedClaims)
            .HasColumnName("allowed_claims")
            .HasMaxLength(1000)
            .IsRequired();
        _ = builder
            .Property(x => x.AllowUserApiAccess)
            .HasColumnName("allow_user_api_access")
            .IsRequired();
        _ = builder
            .Property(x => x.ScopeApprovalExpirationDays)
            .HasColumnName("scope_approval_expiration_days")
            .IsRequired();
        _ = builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }
}
