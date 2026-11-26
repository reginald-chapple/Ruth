using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Personalization;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class InteractionChannelConfiguration : IEntityTypeConfiguration<InteractionChannel>
{
    public void Configure(EntityTypeBuilder<InteractionChannel> builder)
    {
        _ = builder.ToTable("interaction_channels");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.ChannelType).HasConversion<string>().HasMaxLength(32);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Key).IsUnique();
    }
}
