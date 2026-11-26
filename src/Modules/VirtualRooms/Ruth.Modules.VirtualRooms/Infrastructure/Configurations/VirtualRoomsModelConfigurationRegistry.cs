using Microsoft.EntityFrameworkCore;

namespace Ruth.Modules.VirtualRooms.Infrastructure.Configurations;

internal static class VirtualRoomsModelConfigurationRegistry
{
    public static void ApplyAll(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfiguration(new VirtualRoomConfiguration());
        _ = modelBuilder.ApplyConfiguration(new VirtualRoomSessionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new VirtualRoomParticipantSessionConfiguration());
        _ = modelBuilder.ApplyConfiguration(new VirtualRoomRecordingArchiveConfiguration());
    }
}
