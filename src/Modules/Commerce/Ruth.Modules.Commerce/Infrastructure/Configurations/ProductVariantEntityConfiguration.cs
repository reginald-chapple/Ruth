using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Catalog;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class ProductVariantEntityConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        _ = builder.ToTable("product_variants");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Id).ValueGeneratedNever();
        _ = builder.Property(static x => x.Sku).HasMaxLength(64).IsRequired();
        _ = builder.Property(static x => x.Name).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.UnitPrice).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.FairMarketValueAmount).HasPrecision(18, 2).IsRequired();
        _ = builder.HasIndex(static x => x.Sku).IsUnique();
        _ = builder.HasIndex(static x => x.ProductId);
    }
}
