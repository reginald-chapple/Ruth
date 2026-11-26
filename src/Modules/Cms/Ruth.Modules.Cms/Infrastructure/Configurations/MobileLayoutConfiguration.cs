using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Channels;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class MobileLayoutConfiguration : IEntityTypeConfiguration<MobileLayout>
{
    public void Configure(EntityTypeBuilder<MobileLayout> builder)
    {
        _ = builder.ToTable("mobile_layouts");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.LayoutKey).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.ShellMarkup).HasColumnType("text").IsRequired();
        _ = builder.HasIndex(static x => new { x.MobileApplicationId, x.LayoutKey }).IsUnique();
    }
}
