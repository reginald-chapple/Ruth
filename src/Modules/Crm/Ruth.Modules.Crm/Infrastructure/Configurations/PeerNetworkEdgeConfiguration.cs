using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Crm.Domain.Relationships;

namespace Ruth.Modules.Crm.Infrastructure.Configurations;

internal sealed class PeerNetworkEdgeConfiguration : IEntityTypeConfiguration<PeerNetworkEdge>
{
    public void Configure(EntityTypeBuilder<PeerNetworkEdge> builder)
    {
        _ = builder.ToTable("peer_network_edges");
        _ = builder.HasKey(static e => e.Id);

        _ = builder
            .Property(static e => e.RelationshipType)
            .HasConversion<string>()
            .HasMaxLength(32);
        _ = builder.Property(static e => e.Strength).HasConversion<string>().HasMaxLength(16);
        _ = builder.Property(static e => e.Trend).HasConversion<string>().HasMaxLength(16);
        _ = builder.Property(static e => e.RelationshipScore).HasPrecision(8, 1);
        _ = builder.Property(static e => e.PreviousScore).HasPrecision(8, 1);
        _ = builder.Property(static e => e.Caption).HasMaxLength(200).IsRequired();

        _ = builder.HasIndex(static e => new
        {
            e.SourcePersonId,
            e.TargetPersonId,
            e.RelationshipType,
            e.RelatedEntityId,
        });
        _ = builder.HasIndex(static e => e.TargetPersonId);
    }
}
