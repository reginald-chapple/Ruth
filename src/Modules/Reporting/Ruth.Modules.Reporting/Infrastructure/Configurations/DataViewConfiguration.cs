using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Reporting.Domain.DataViews;

namespace Ruth.Modules.Reporting.Infrastructure.Configurations;

internal sealed class DataViewConfiguration : IEntityTypeConfiguration<DataView>
{
    public void Configure(EntityTypeBuilder<DataView> builder)
    {
        _ = builder.ToTable("data_views");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(180).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(1000);
        _ = builder.Property(static x => x.EntityTypeKey).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.TransformKey).HasMaxLength(120);
        _ = builder
            .HasMany(static x => x.Filters)
            .WithOne()
            .HasForeignKey(static f => f.DataViewId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Filters)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.EntityTypeKey);
    }
}
