using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Cms.Domain.Engine;

namespace Ruth.Modules.Cms.Infrastructure.Configurations;

internal sealed class PageRouteConfiguration : IEntityTypeConfiguration<PageRoute>
{
    public void Configure(EntityTypeBuilder<PageRoute> builder)
    {
        _ = builder.ToTable("page_routes");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Route).HasMaxLength(260).IsRequired();
        _ = builder.HasIndex(static x => x.PageId);
        _ = builder.HasIndex(static x => x.Route);
    }
}
