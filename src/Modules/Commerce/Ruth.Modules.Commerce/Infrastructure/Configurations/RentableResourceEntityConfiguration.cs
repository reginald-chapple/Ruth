using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class RentableResourceEntityConfiguration
    : IEntityTypeConfiguration<RentableResource>
{
    public void Configure(EntityTypeBuilder<RentableResource> builder)
    {
        _ = builder.ToTable("rentable_resources");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder
            .Property(static x => x.ResourceKind)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.HourlyRate).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.FlatCleaningOrSetupFee).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.MemberHourlyRate).HasPrecision(18, 2);
        _ = builder.Property(static x => x.FreeOrDiscountedMemberTier).HasMaxLength(80);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => x.CoreLocationId);
    }
}
