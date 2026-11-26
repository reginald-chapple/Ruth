using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Content;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class ContentTopicConfiguration : IEntityTypeConfiguration<ContentTopic>
{
    public void Configure(EntityTypeBuilder<ContentTopic> builder)
    {
        _ = builder.ToTable("content_topics");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
    }
}
