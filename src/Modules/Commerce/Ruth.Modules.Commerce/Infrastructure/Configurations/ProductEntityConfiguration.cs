using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Catalog;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class ProductEntityConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        _ = builder.ToTable("products");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder
            .Property(static x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.FlatTaxRatePercent).HasPrecision(8, 4).IsRequired();
        _ = builder.Property(static x => x.FlatShippingFeeAmount).HasPrecision(18, 2).IsRequired();

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder
            .HasMany(static x => x.Variants)
            .WithOne()
            .HasForeignKey(static x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Variants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new
        {
            x.IsActive,
            x.IsPublic,
            x.Kind,
        });
    }
}
