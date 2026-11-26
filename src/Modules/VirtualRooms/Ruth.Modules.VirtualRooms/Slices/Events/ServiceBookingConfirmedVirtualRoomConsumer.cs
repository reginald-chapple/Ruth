using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.VirtualRooms.Contracts.Rooms;
using Ruth.Modules.VirtualRooms.Domain.Rooms;
using Ruth.Modules.VirtualRooms.Infrastructure;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.VirtualRooms.Slices.Events;

/// <summary>Automatically provisions an embedded VirtualRoom and scheduled VirtualRoomSession upon ServiceBookingConfirmedIntegrationEvent.</summary>
internal sealed class ServiceBookingConfirmedVirtualRoomConsumer(VirtualRoomsDbContext dbContext)
    : IIntegrationEventConsumer<ServiceBookingConfirmedIntegrationEvent>
{
    public async Task ConsumeAsync(
        ServiceBookingConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (!integrationEvent.RequiresVirtualRoom)
        {
            return;
        }

        var room = await EnsureBookingRoomAsync(integrationEvent, cancellationToken);
        if (room is null)
        {
            return;
        }

        await EnsureBookingSessionAsync(room, integrationEvent, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<VirtualRoom?> EnsureBookingRoomAsync(
        ServiceBookingConfirmedIntegrationEvent evt,
        CancellationToken ct
    )
    {
        var refKey = string.IsNullOrWhiteSpace(evt.VirtualRoomReferenceKey)
            ? $"booking-{evt.BookingId:N}"
            : evt.VirtualRoomReferenceKey.Trim();

        var existing = await dbContext.VirtualRooms.FirstOrDefaultAsync(
            r => r.ExternalReferenceKey == refKey,
            ct
        );
        if (existing is not null)
        {
            return existing;
        }

        var slug = $"booking-{evt.BookingId.ToString("N")[..12]}";
        var maxParticipants = Math.Max(2, evt.ParticipantOrUnitCount + 2);
        var created = VirtualRoom.Create(
            new VirtualRoomSpec(
                $"{evt.TargetName} ({evt.ConfirmationCode})",
                slug,
                VirtualRoomContextKind.CommerceBooking,
                Description: $"Provisioned for {evt.CustomerName}",
                LinkedLocationId: evt.LocationId,
                LinkedBranchId: evt.BranchId,
                LinkedBookingId: evt.BookingId,
                ExternalReferenceKey: refKey,
                HostModeratorPersonId: evt.ProviderPersonId,
                MaxParticipants: maxParticipants,
                AutoRecordAttendance: false,
                MinimumAttendanceDwellSeconds: 60
            )
        );
        if (created.IsFailure)
        {
            return null;
        }

        _ = dbContext.VirtualRooms.Add(created.Value);
        return created.Value;
    }

    private async Task EnsureBookingSessionAsync(
        VirtualRoom room,
        ServiceBookingConfirmedIntegrationEvent evt,
        CancellationToken ct
    )
    {
        var sessionExists = await dbContext.VirtualRoomSessions.AnyAsync(
            s =>
                s.VirtualRoomId == room.Id && s.ScheduledStartUtc == evt.StartUtc.ToUniversalTime(),
            ct
        );
        if (sessionExists)
        {
            return;
        }

        var sessionRes = VirtualRoomSession.Create(
            room,
            $"{evt.TargetName} — {evt.CustomerName}",
            evt.StartUtc,
            evt.EndUtc
        );
        if (sessionRes.IsSuccess)
        {
            _ = dbContext.VirtualRoomSessions.Add(sessionRes.Value);
        }
    }
}
