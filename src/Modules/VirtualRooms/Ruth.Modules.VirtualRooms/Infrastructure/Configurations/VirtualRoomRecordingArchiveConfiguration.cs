using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.VirtualRooms.Domain.Rooms;

namespace Ruth.Modules.VirtualRooms.Infrastructure.Configurations;

internal sealed class VirtualRoomRecordingArchiveConfiguration
    : IEntityTypeConfiguration<VirtualRoomRecordingArchive>
{
    public void Configure(EntityTypeBuilder<VirtualRoomRecordingArchive> builder)
    {
        _ = builder.ToTable("virtual_room_recording_archives");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.EgressId).HasMaxLength(120).IsRequired();
        _ = builder.Property(static x => x.Status).HasConversion<string>().HasMaxLength(32);
        _ = builder.Property(static x => x.StorageBucket).HasMaxLength(128).IsRequired();
        _ = builder.Property(static x => x.StorageObjectKey).HasMaxLength(512).IsRequired();
        _ = builder.Property(static x => x.TranscriptObjectKey).HasMaxLength(512);
        _ = builder.Property(static x => x.ContentType).HasMaxLength(80).IsRequired();
        _ = builder.Property(static x => x.ErrorMessage).HasMaxLength(1000);

        _ = builder.HasIndex(static x => x.EgressId);
        _ = builder.HasIndex(static x => new { x.SessionId, x.Status });
    }
}
