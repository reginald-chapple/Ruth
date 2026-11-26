using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Content;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class ContentChannelTypeConfiguration : IEntityTypeConfiguration<ContentChannelType>
{
    public void Configure(EntityTypeBuilder<ContentChannelType> builder)
    {
        _ = builder.ToTable("content_channel_types");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Key).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.DateRangeType).HasConversion<string>().HasMaxLength(24);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Key).IsUnique();
    }
}
