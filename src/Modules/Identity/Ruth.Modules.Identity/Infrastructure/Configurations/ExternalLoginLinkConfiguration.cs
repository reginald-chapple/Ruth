using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class ExternalLoginLinkConfiguration : IEntityTypeConfiguration<ExternalLoginLink>
{
    public void Configure(EntityTypeBuilder<ExternalLoginLink> builder)
    {
        _ = builder.ToTable("external_login_links");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id").IsRequired();
        _ = builder
            .Property(x => x.ProviderKey)
            .HasColumnName("provider_key")
            .HasMaxLength(AuthenticationPolicy.ProviderKeyMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.ProviderSubject)
            .HasColumnName("provider_subject")
            .HasMaxLength(256)
            .IsRequired();
        _ = builder
            .Property(x => x.ProviderEmail)
            .HasColumnName("provider_email")
            .HasMaxLength(OnboardingPolicy.EmailMaxLength);
        _ = builder.Property(x => x.LinkedOnUtc).HasColumnName("linked_on_utc").IsRequired();

        _ = builder.HasIndex(x => new { x.ProviderKey, x.ProviderSubject }).IsUnique();
        _ = builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
