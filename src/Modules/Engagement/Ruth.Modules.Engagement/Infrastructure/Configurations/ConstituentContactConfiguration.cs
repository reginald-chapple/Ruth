using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class ConstituentContactConfiguration : IEntityTypeConfiguration<ConstituentContact>
{
    public void Configure(EntityTypeBuilder<ConstituentContact> builder)
    {
        _ = builder.ToTable("constituent_contacts");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.PortfolioRole).HasMaxLength(80).IsRequired();
        _ = builder
            .Property(static x => x.RelationshipStrength)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.PreferredChannel)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder
            .Property(static x => x.LatestSentiment)
            .HasConversion<string>()
            .HasMaxLength(24);
        _ = builder
            .Property(static x => x.HealthStatus)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        _ = builder.Property(static x => x.Notes).HasMaxLength(2000);

        JsonbAttributeConventions.ConfigureJsonbDictionary(
            builder,
            static x => x.AttributeValues,
            columnName: "attribute_values",
            createGinIndex: true
        );

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.OwnerPersonId, x.ConstituentPersonId }).IsUnique();
        _ = builder.HasIndex(static x => new
        {
            x.OwnerPersonId,
            x.IsActive,
            x.HealthScore,
        });
    }
}
