using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class SecurityRoleConfiguration : IEntityTypeConfiguration<SecurityRole>
{
    public void Configure(EntityTypeBuilder<SecurityRole> builder)
    {
        _ = builder.ToTable("security_roles");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(OnboardingPolicy.RoleNameMaxLength)
            .IsRequired();
        _ = builder
            .Property(x => x.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(OnboardingPolicy.RoleNameMaxLength)
            .IsRequired();
        _ = builder.HasIndex(x => x.NormalizedName).IsUnique();

        _ = builder.Property(x => x.IsSystem).HasColumnName("is_system").IsRequired();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();
    }
}
