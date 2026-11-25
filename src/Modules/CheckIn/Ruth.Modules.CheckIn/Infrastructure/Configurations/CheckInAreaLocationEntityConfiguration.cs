using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.CheckIn.Domain.Configurations;

namespace Ruth.Modules.CheckIn.Infrastructure.Configurations;

internal sealed class CheckInAreaLocationEntityConfiguration
    : IEntityTypeConfiguration<CheckInAreaLocation>
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CheckInAreaLocation> builder)
    {
        _ = builder.ToTable("check_in_area_locations");
        _ = builder.HasKey(static r => r.Id);
        _ = builder.Property(static r => r.Id).ValueGeneratedNever();

        _ = builder.Property(static r => r.LocationName).HasMaxLength(120).IsRequired();

        var schedulesComparer = new ValueComparer<IReadOnlyList<Guid>>(
            static (left, right) => EqualsGuidList(left, right),
            static list => ComputeGuidListHash(list),
            static list => list.ToArray()
        );

        _ = builder
            .Property(static r => r.ScheduleIds)
            .HasColumnName("schedule_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                static ids => SerializeGuidList(ids),
                static json => DeserializeGuidList(json),
                schedulesComparer
            )
            .IsRequired();

        _ = builder.HasIndex(static r => new { r.CheckInAreaId, r.LocationId }).IsUnique();
        _ = builder.HasIndex(static r => r.LocationId);
    }

    private static string SerializeGuidList(IReadOnlyList<Guid> ids)
    {
        return JsonSerializer.Serialize(ids, _jsonOptions);
    }

    private static Guid[] DeserializeGuidList(string json)
    {
        return string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<Guid[]>(json, _jsonOptions) ?? [];
    }

    private static bool EqualsGuidList(IReadOnlyList<Guid>? left, IReadOnlyList<Guid>? right)
    {
        return ReferenceEquals(left, right)
            || (left is not null && right is not null && left.SequenceEqual(right));
    }

    private static int ComputeGuidListHash(IReadOnlyList<Guid> list)
    {
        var hash = new HashCode();
        foreach (var id in list)
        {
            hash.Add(id);
        }

        return hash.ToHashCode();
    }
}
