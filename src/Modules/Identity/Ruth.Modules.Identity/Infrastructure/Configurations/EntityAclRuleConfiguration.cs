using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class EntityAclRuleConfiguration : IEntityTypeConfiguration<EntityAclRule>
{
    public void Configure(EntityTypeBuilder<EntityAclRule> builder)
    {
        _ = builder.ToTable("entity_acl_rules");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.EntityTypeKey)
            .HasColumnName("entity_type_key")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        _ = builder.Property(x => x.Order).HasColumnName("order").IsRequired();
        _ = builder.Property(x => x.Decision).HasColumnName("decision").IsRequired();
        _ = builder.Property(x => x.SpecialRole).HasColumnName("special_role").IsRequired();
        _ = builder.Property(x => x.SecurityRoleId).HasColumnName("security_role_id");
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id");
        _ = builder.Property(x => x.CreatedOnUtc).HasColumnName("created_on_utc").IsRequired();
        _ = builder.Property(x => x.ModifiedOnUtc).HasColumnName("modified_on_utc");
        _ = builder
            .Property(x => x.ConcurrencyStamp)
            .HasColumnName("concurrency_stamp")
            .IsRequired();

        _ = builder.HasIndex(x => new
        {
            x.EntityTypeKey,
            x.EntityId,
            x.Action,
            x.Order,
        });

        _ = builder
            .HasOne<SecurityRole>()
            .WithMany()
            .HasForeignKey(x => x.SecurityRoleId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder
            .HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(x => x.UserAccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
