using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        _ = builder.ToTable("user_roles");
        _ = builder.HasKey(x => new { x.UserAccountId, x.SecurityRoleId });

        _ = builder
            .Property(x => x.UserAccountId)
            .HasColumnName("user_account_id")
            .ValueGeneratedNever();
        _ = builder
            .Property(x => x.SecurityRoleId)
            .HasColumnName("security_role_id")
            .ValueGeneratedNever();
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");

        _ = builder
            .HasOne<SecurityRole>()
            .WithMany()
            .HasForeignKey(x => x.SecurityRoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
