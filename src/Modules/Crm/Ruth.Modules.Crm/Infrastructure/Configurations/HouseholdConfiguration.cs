using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Households;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class HouseholdConfiguration : IEntityTypeConfiguration<Household>
{
    public void Configure(EntityTypeBuilder<Household> builder)
    {
        _ = builder.ToTable("households");
        _ = builder.HasKey(static h => h.Id);

        _ = builder.Property(static h => h.Name).HasMaxLength(100).IsRequired();
        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static h => h.Attributes,
            "attributes",
            createGinIndex: true
        );
        _ = builder.Ignore(static h => h.AttributeValues);

        _ = builder
            .HasMany(static h => h.Members)
            .WithOne()
            .HasForeignKey(static m => m.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .HasMany(static h => h.Locations)
            .WithOne()
            .HasForeignKey(static l => l.HouseholdId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder
            .Navigation(static h => h.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        _ = builder
            .Navigation(static h => h.Locations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.HasIndex(static h => h.BranchId);
        _ = builder.HasIndex(static h => h.IsArchived);
    }
}
