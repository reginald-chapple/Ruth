using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        _ = builder.ToTable("user_accounts");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        ConfigureIdentityProperties(builder);
        ConfigureAuditProperties(builder);
        ConfigureRoleRelationship(builder);
    }

    private static void ConfigureIdentityProperties(EntityTypeBuilder<UserAccount> builder)
    {
        _ = builder
            .Property(x => x.UserName)
            .HasColumnName("user_name")
            .HasMaxLength(OnboardingPolicy.UserNameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.NormalizedUserName)
            .HasColumnName("normalized_user_name")
            .HasMaxLength(OnboardingPolicy.UserNameMaxLength)
            .IsRequired();
        _ = builder.HasIndex(x => x.NormalizedUserName).IsUnique();

        _ = builder
            .Property(x => x.Email)
            .HasColumnName("email")
            .HasMaxLength(OnboardingPolicy.EmailMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(OnboardingPolicy.EmailMaxLength)
            .IsRequired();
        _ = builder.HasIndex(x => x.NormalizedEmail).IsUnique();

        _ = builder
            .Property(x => x.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(OnboardingPolicy.DisplayNameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();
    }

    private static void ConfigureAuditProperties(EntityTypeBuilder<UserAccount> builder)
    {
        _ = builder.Property(x => x.IsConfirmed).HasColumnName("is_confirmed").IsRequired();
        _ = builder.Property(x => x.IsLockedOut).HasColumnName("is_locked_out").IsRequired();
        _ = builder
            .Property(x => x.LastPasswordChangedOnUtc)
            .HasColumnName("last_password_changed_on_utc");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }

    private static void ConfigureRoleRelationship(EntityTypeBuilder<UserAccount> builder)
    {
        _ = builder
            .HasMany(x => x.Roles)
            .WithOne()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder.Navigation(x => x.Roles).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
