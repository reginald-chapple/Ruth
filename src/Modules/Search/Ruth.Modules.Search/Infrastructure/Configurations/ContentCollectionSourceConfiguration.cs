using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Search.Domain.Collections;

namespace Ruth.Modules.Search.Infrastructure.Configurations;

internal sealed class ContentCollectionSourceConfiguration
    : IEntityTypeConfiguration<ContentCollectionSource>
{
    public void Configure(EntityTypeBuilder<ContentCollectionSource> builder)
    {
        _ = builder.ToTable("content_collection_sources");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.ProviderKey).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.EntityTypeKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.FilterConfigJson).HasColumnType("jsonb");
        _ = builder.HasIndex(static x => new { x.ContentCollectionId, x.ProviderKey }).IsUnique();
    }
}
