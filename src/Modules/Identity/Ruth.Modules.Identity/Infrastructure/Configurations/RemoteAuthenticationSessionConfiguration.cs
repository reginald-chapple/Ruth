using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class RemoteAuthenticationSessionConfiguration
    : IEntityTypeConfiguration<RemoteAuthenticationSession>
{
    public void Configure(EntityTypeBuilder<RemoteAuthenticationSession> builder)
    {
        _ = builder.ToTable("remote_authentication_sessions");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(AuthenticationPolicy.ProviderKeyMaxLength)
            .IsRequired();
        _ = builder.Property(x => x.State).HasColumnName("state").HasMaxLength(256).IsRequired();
        _ = builder.HasIndex(x => x.State).IsUnique();
        _ = builder.Property(x => x.Nonce).HasColumnName("nonce").HasMaxLength(256).IsRequired();
        _ = builder
            .Property(x => x.CodeVerifier)
            .HasColumnName("code_verifier")
            .HasMaxLength(256)
            .IsRequired();
        _ = builder
            .Property(x => x.ReturnUrl)
            .HasColumnName("return_url")
            .HasMaxLength(AuthenticationPolicy.UrlMaxLength);
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ExpiresOnUtc).HasColumnName("expires_on_utc").IsRequired();
        _ = builder.Property(x => x.CompletedOnUtc).HasColumnName("completed_on_utc");
    }
}
