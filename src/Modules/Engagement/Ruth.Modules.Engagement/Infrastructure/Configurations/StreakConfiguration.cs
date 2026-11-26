using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Engagement.Domain.Streaks;

namespace Ruth.Modules.Engagement.Infrastructure.Configurations;

internal sealed class StreakConfiguration : IEntityTypeConfiguration<Streak>
{
    public void Configure(EntityTypeBuilder<Streak> builder)
    {
        _ = builder.ToTable("streaks");
        _ = builder.HasKey(static x => x.Id);

        var byteComparer = new ValueComparer<byte[]>(
            static (a, b) => ByteArraysEqual(a, b),
            static a => ComputeBytesHash(a),
            static a => a.ToArray()
        );

        var mapProp = builder
            .Property(static x => x.EngagementMap)
            .HasColumnName("engagement_map")
            .HasColumnType("bytea")
            .IsRequired();
        mapProp.Metadata.SetValueComparer(byteComparer);

        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.StreakTypeId, x.PersonId }).IsUnique();
        _ = builder.HasIndex(static x => x.PersonId);
    }

    private static bool ByteArraysEqual(byte[]? left, byte[]? right)
    {
        return (left ?? []).AsSpan().SequenceEqual(right ?? []);
    }

    private static int ComputeBytesHash(byte[] bytes)
    {
        var hash = new HashCode();
        hash.AddBytes(bytes);
        return hash.ToHashCode();
    }
}
