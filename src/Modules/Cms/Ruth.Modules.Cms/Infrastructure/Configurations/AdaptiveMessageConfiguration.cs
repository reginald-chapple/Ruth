using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class AdaptiveMessageConfiguration : IEntityTypeConfiguration<AdaptiveMessage>
{
    public void Configure(EntityTypeBuilder<AdaptiveMessage> builder)
    {
        _ = builder.ToTable("adaptive_messages");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder
            .HasMany(static x => x.Adaptations)
            .WithOne()
            .HasForeignKey(static a => a.AdaptiveMessageId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Adaptations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Key).IsUnique();
    }
}
