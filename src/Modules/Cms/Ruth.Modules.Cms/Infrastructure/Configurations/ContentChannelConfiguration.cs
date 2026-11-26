using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class ContentChannelConfiguration : IEntityTypeConfiguration<ContentChannel>
{
    public void Configure(EntityTypeBuilder<ContentChannel> builder)
    {
        _ = builder.ToTable("content_channels");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.IconCssClass).HasMaxLength(120);
        _ = builder.Property(static x => x.ChannelUrl).HasMaxLength(512);
        _ = builder.Property(static x => x.ItemUrl).HasMaxLength(512);
        CmsJsonbConverters.ConfigureJsonbList(
            builder,
            static x => x.ChildChannelIds,
            "child_channel_ids"
        );
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            "attribute_values",
            createGinIndex: true
        );
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
