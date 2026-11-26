using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class HtmlContentConfiguration : IEntityTypeConfiguration<HtmlContent>
{
    public void Configure(EntityTypeBuilder<HtmlContent> builder)
    {
        _ = builder.ToTable("html_contents");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.EntityValue).HasMaxLength(120);
        _ = builder.Property(static x => x.Content).HasColumnType("text").IsRequired();
        _ = builder.Property(static x => x.ContentSummary).HasMaxLength(500);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.BlockId, x.Version });
    }
}
