using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Relationships;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class KnownRelationshipConfiguration : IEntityTypeConfiguration<KnownRelationship>
{
    public void Configure(EntityTypeBuilder<KnownRelationship> builder)
    {
        _ = builder.ToTable("known_relationships");
        _ = builder.HasKey(static r => r.Id);

        _ = builder
            .Property(static r => r.RelationshipType)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Property(static r => r.Notes).HasMaxLength(500);

        _ = builder
            .HasIndex(static r => new
            {
                r.OwnerPersonId,
                r.RelatedPersonId,
                r.RelationshipType,
            })
            .IsUnique();
        _ = builder.HasIndex(static r => r.RelatedPersonId);
        _ = builder.HasIndex(static r => r.InverseRelationshipId);
    }
}
