using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.VirtualRooms.Domain.Rooms;

namespace Ruth.Modules.VirtualRooms.Infrastructure.Configurations;

internal sealed class VirtualRoomParticipantSessionConfiguration
    : IEntityTypeConfiguration<VirtualRoomParticipantSession>
{
    public void Configure(EntityTypeBuilder<VirtualRoomParticipantSession> builder)
    {
        _ = builder.ToTable("virtual_room_participant_sessions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.ParticipantIdentity).HasMaxLength(160).IsRequired();
        _ = builder.Property(static x => x.DisplayName).HasMaxLength(200).IsRequired();
        _ = builder.Property(static x => x.Role).HasConversion<string>().HasMaxLength(32);

        _ = builder.HasIndex(static x => new { x.SessionId, x.ParticipantIdentity });
        _ = builder.HasIndex(static x => x.PersonId);
    }
}
