using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Search.Domain.Indexing;

namespace Ruth.Modules.Search.Infrastructure.Configurations;

internal sealed class SearchDocumentConfiguration : IEntityTypeConfiguration<SearchDocument>
{
    public void Configure(EntityTypeBuilder<SearchDocument> builder)
    {
        _ = builder.ToTable("search_documents");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.SourceKey).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.ModuleName).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.EntityTypeKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.Title).HasMaxLength(300).IsRequired();
        _ = builder.Property(static x => x.Subtitle).HasMaxLength(500);
        _ = builder.Property(static x => x.Summary).HasMaxLength(2000);
        _ = builder.Property(static x => x.TagsText).HasMaxLength(1000).IsRequired();
        _ = builder.Property(static x => x.FacetsJson).HasColumnType("jsonb").IsRequired();
        _ = builder.Property(static x => x.UrlPath).HasMaxLength(500);
        _ = builder.Property(static x => x.SecurityEntityTypeKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.ContentHash).HasMaxLength(64).IsRequired();
        _ = builder.Ignore(static x => x.DomainEvents);

        _ = builder.HasIndex(static x => new
        {
            x.ContentCollectionId,
            x.EntityTypeKey,
            x.EntityId,
        });
        _ = builder.HasIndex(static x => x.ModuleName);
        _ = builder.HasIndex(static x => x.IsPublic);
    }
}
