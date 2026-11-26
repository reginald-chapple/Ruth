using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ruth.Modules.VirtualRooms.Domain.Rooms;

namespace Ruth.Modules.VirtualRooms.Infrastructure.Configurations;

internal sealed class VirtualRoomSessionConfiguration : IEntityTypeConfiguration<VirtualRoomSession>
{
    public void Configure(EntityTypeBuilder<VirtualRoomSession> builder)
    {
        _ = builder.ToTable("virtual_room_sessions");
        _ = builder.HasKey(static x => x.Id);
        _ = builder.Property(static x => x.LiveKitRoomSid).HasMaxLength(96);
        _ = builder.Property(static x => x.Title).HasMaxLength(250).IsRequired();
        _ = builder.Property(static x => x.Status).HasConversion<string>().HasMaxLength(32);

        _ = builder
            .HasMany(static x => x.Participants)
            .WithOne()
            .HasForeignKey(static p => p.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder
            .HasMany(static x => x.Recordings)
            .WithOne()
            .HasForeignKey(static r => r.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
        _ = builder
            .Navigation(static x => x.Recordings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        _ = builder.Ignore(static x => x.IsClosed);
        _ = builder.Ignore(static x => x.DomainEvents);
        _ = builder.HasIndex(static x => new { x.VirtualRoomId, x.Status });
        _ = builder.HasIndex(static x => x.LiveKitRoomSid);
    }
}
