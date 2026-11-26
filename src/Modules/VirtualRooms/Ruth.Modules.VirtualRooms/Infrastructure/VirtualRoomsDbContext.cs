using Microsoft.EntityFrameworkCore;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Persistence.EntityFramework;

namespace Ruth.Modules.VirtualRooms.Infrastructure;

/// <summary>EF Core database context exclusively owning the virtual PostgreSQL schema.</summary>
public sealed class VirtualRoomsDbContext(DbContextOptions<VirtualRoomsDbContext> options)
    : ModuleDbContext(options)
{
    /// <summary>PostgreSQL schema name owned by the VirtualRooms module.</summary>
    public const string SchemaName = "virtual";

    /// <inheritdoc />
    protected override string Schema => SchemaName;

    public DbSet<VirtualRoom> VirtualRooms => Set<VirtualRoom>();
    public DbSet<VirtualRoomSession> VirtualRoomSessions => Set<VirtualRoomSession>();
    public DbSet<VirtualRoomParticipantSession> VirtualRoomParticipantSessions =>
        Set<VirtualRoomParticipantSession>();
    public DbSet<VirtualRoomRecordingArchive> VirtualRoomRecordingArchives =>
        Set<VirtualRoomRecordingArchive>();

    /// <inheritdoc />
    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        Configurations.VirtualRoomsModelConfigurationRegistry.ApplyAll(modelBuilder);
    }
}
