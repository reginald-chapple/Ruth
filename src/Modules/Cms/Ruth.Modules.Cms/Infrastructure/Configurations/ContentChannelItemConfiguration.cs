using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Content;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class ContentChannelItemConfiguration : IEntityTypeConfiguration<ContentChannelItem>
{
    public void Configure(EntityTypeBuilder<ContentChannelItem> builder)
    {
        _ = builder.ToTable("content_channel_items");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Title).HasMaxLength(240).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(1000);
        _ = builder.Property(static x => x.Content).HasColumnType("text").IsRequired();
        _ = builder.Property(static x => x.Status).HasConversion<string>().HasMaxLength(32);
        CmsJsonbConverters.ConfigureJsonbList(builder, static x => x.TopicIds, "topic_ids");
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            "attribute_values",
            createGinIndex: true
        );
        _ = builder
            .HasMany(static x => x.ChildAssociations)
            .WithOne()
            .HasForeignKey(static a => a.ParentItemId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.ChildAssociations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.ContentChannelId, x.Slug }).IsUnique();
        _ = builder.HasIndex(static x => new { x.ContentChannelId, x.StartDateTimeUtc });
    }
}
