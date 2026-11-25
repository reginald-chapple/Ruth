using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Identity.Domain;

namespace Ruth.Modules.Identity.Infrastructure.Configurations;

internal sealed class AuthAuditLogConfiguration : IEntityTypeConfiguration<AuthAuditLog>
{
    public void Configure(EntityTypeBuilder<AuthAuditLog> builder)
    {
        _ = builder.ToTable("auth_audit_logs");
        _ = builder.HasKey(x => x.Id);

        _ = builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        _ = builder
            .Property(x => x.EntityTypeKey)
            .HasColumnName("entity_type_key")
            .HasMaxLength(200)
            .IsRequired();
        _ = builder.Property(x => x.EntityId).HasColumnName("entity_id").IsRequired();
        _ = builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
        _ = builder.Property(x => x.ChangeType).HasColumnName("change_type").IsRequired();
        _ = builder.Property(x => x.ChangedOnUtc).HasColumnName("changed_on_utc").IsRequired();
        _ = builder.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id");
        _ = builder.Property(x => x.PreDecision).HasColumnName("pre_decision");
        _ = builder.Property(x => x.PostDecision).HasColumnName("post_decision");
        _ = builder.Property(x => x.PreOrder).HasColumnName("pre_order");
        _ = builder.Property(x => x.PostOrder).HasColumnName("post_order");
        _ = builder.Property(x => x.SpecialRole).HasColumnName("special_role").IsRequired();
        _ = builder.Property(x => x.SecurityRoleId).HasColumnName("security_role_id");
        _ = builder.Property(x => x.UserAccountId).HasColumnName("user_account_id");

        _ = builder.HasIndex(x => x.ChangedOnUtc);
        _ = builder.HasIndex(x => new { x.EntityTypeKey, x.EntityId });
    }
}
