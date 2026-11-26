using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Commerce.Infrastructure.Configurations;

internal sealed class BookableServiceEntityConfiguration : IEntityTypeConfiguration<BookableService>
{
    public void Configure(EntityTypeBuilder<BookableService> builder)
    {
        _ = builder.ToTable("bookable_services");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.Name).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Slug).HasMaxLength(96).IsRequired();
        _ = builder.Property(static x => x.Summary).HasMaxLength(500);
        _ = builder.Property(static x => x.Description).HasMaxLength(4000);
        _ = builder
            .Property(static x => x.DeliveryMode)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        _ = builder.Property(static x => x.StandardPrice).HasPrecision(18, 2).IsRequired();
        _ = builder.Property(static x => x.MemberOverridePrice).HasPrecision(18, 2);
        _ = builder.Property(static x => x.FreeOrDiscountedMemberTier).HasMaxLength(80);

        CommerceJsonbListConventions.ConfigureGuidList(
            builder,
            static x => x.AssignedProviderPersonIds,
            "assigned_provider_person_ids"
        );
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => x.Slug).IsUnique();
        _ = builder.HasIndex(static x => new { x.IsActive, x.IsPublic });
    }
}
