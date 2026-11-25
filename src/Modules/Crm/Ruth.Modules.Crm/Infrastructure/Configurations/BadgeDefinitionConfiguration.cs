using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class BadgeDefinitionConfiguration : IEntityTypeConfiguration<BadgeDefinition>
{
    public void Configure(EntityTypeBuilder<BadgeDefinition> builder)
    {
        _ = builder.ToTable("badge_definitions");
        _ = builder.HasKey(static b => b.Id);

        _ = builder.Property(static b => b.Name).HasMaxLength(100).IsRequired();
        _ = builder.Property(static b => b.Description).HasMaxLength(2000);
        _ = builder.Property(static b => b.BadgeComponentKey).HasMaxLength(100).IsRequired();
        _ = builder.Property(static b => b.TargetEntityTypeKey).HasMaxLength(150).IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static b => b.Settings,
            "settings",
            createGinIndex: false
        );

        _ = builder.HasIndex(static b => new { b.IsActive, b.Order });
    }
}
