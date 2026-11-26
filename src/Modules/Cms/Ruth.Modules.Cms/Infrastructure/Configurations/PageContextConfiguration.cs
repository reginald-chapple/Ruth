using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PageContextConfiguration : IEntityTypeConfiguration<PageContext>
{
    public void Configure(EntityTypeBuilder<PageContext> builder)
    {
        _ = builder.ToTable("page_contexts");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.EntityKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.IdParameter).HasMaxLength(80).IsRequired();
        _ = builder.HasIndex(static x => new { x.PageId, x.EntityKey }).IsUnique();
    }
}
