using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.Groups.Domain.Groups;

namespace Ruth.Modules.Groups.Infrastructure.Configurations;

internal sealed class GroupLocationConfiguration : IEntityTypeConfiguration<GroupLocation>
{
    private static readonly ValueComparer<List<Guid>> _guidListComparer = new(
        static (left, right) =>
            (left == null && right == null)
            || (left != null && right != null && left.SequenceEqual(right)),
        static list => list.Aggregate(0, static (acc, id) => HashCode.Combine(acc, id)),
        static list => list.ToList()
    );

    public void Configure(EntityTypeBuilder<GroupLocation> builder)
    {
        _ = builder.ToTable("group_locations");
        _ = builder.HasKey(static gl => gl.Id);
        _ = builder.Property(static gl => gl.Id).ValueGeneratedNever();
        _ = builder.Property(static gl => gl.LocationName).HasMaxLength(150).IsRequired();

        _ = builder
            .Property(static gl => gl.GeoPoint)
            .HasColumnName("geo_point")
            .HasColumnType("geography(Point, 4326)");
        _ = builder
            .Property(static gl => gl.GeoFence)
            .HasColumnName("geo_fence")
            .HasColumnType("geography(Polygon, 4326)");
        _ = builder.HasIndex(static gl => gl.GeoPoint).HasMethod("gist");
        _ = builder.HasIndex(static gl => gl.GeoFence).HasMethod("gist");

        _ = builder
            .Property(static gl => gl.ScheduleIdsList)
            .HasColumnName("schedule_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                static list => JsonSerializer.Serialize(list, (JsonSerializerOptions?)null),
                static json =>
                    string.IsNullOrWhiteSpace(json)
                        ? new List<Guid>()
                        : JsonSerializer.Deserialize<List<Guid>>(json, (JsonSerializerOptions?)null)
                            ?? new List<Guid>(),
                _guidListComparer
            );
        _ = builder.Ignore(static gl => gl.ScheduleIds);

        _ = builder.HasIndex(static gl => new { gl.GroupId, gl.LocationId }).IsUnique();
        _ = builder.HasIndex(static gl => gl.LocationId);
    }
}
