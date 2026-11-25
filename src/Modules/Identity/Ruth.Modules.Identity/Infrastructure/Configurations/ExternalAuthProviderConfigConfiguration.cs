using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class ExternalAuthProviderConfigConfiguration
    : IEntityTypeConfiguration<ExternalAuthProviderConfig>
{
    public void Configure(EntityTypeBuilder<ExternalAuthProviderConfig> builder)
    {
        _ = builder.ToTable("external_auth_providers");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(AuthenticationPolicy.ProviderKeyMaxLength)
            .IsRequired();
        _ = builder.HasIndex(x => x.ProviderKey).IsUnique();
        _ = builder
            .Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(AuthenticationPolicy.ProviderDisplayNameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.Authority)
            .HasColumnName("authority")
            .HasMaxLength(AuthenticationPolicy.UrlMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.ClientId)
            .HasColumnName("client_id")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder.Property(x => x.ClientSecret).HasColumnName("client_secret").HasMaxLength(512);
        _ = builder.Property(x => x.Scopes).HasColumnName("scopes").HasMaxLength(300).IsRequired();
        _ = builder.Property(x => x.IsEnabled).HasColumnName("is_enabled").IsRequired();
        _ = builder
            .Property(x => x.AutoLinkByEmail)
            .HasColumnName("auto_link_by_email")
            .IsRequired();
        _ = builder
            .Property(x => x.CallbackPath)
            .HasColumnName("callback_path")
            .HasMaxLength(AuthenticationPolicy.UrlMaxLength)
            .IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }
}
