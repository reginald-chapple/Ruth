using System.Security.Cryptography;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Short-lived TTL reservation hold preventing double-booking while a customer completes checkout.</summary>
public sealed class BookingSlotHold : Entity<BookingSlotHoldId>
{
    private BookingSlotHold() { }

    private BookingSlotHold(BookingSlotHoldId id)
        : base(id) { }

    public string HoldToken { get; private set; } = string.Empty;
    public BookingTargetKind TargetKind { get; private set; }
    public BookableServiceId? BookableServiceId { get; private set; }
    public RentableResourceId? RentableResourceId { get; private set; }
    public Guid? ProviderPersonId { get; private set; }
    public Guid? LocationId { get; private set; }
    public DateTimeOffset SlotStartUtc { get; private set; }
    public DateTimeOffset SlotEndUtc { get; private set; }
    public DateTimeOffset EffectiveBlockStartUtc { get; private set; }
    public DateTimeOffset EffectiveBlockEndUtc { get; private set; }
    public int Quantity { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public bool IsCompleted { get; private set; }
    public ServiceBookingId? CompletedBookingId { get; private set; }

    public static BookingSlotHold Create(
        BookingTargetKind targetKind,
        BookableServiceId? serviceId,
        RentableResourceId? resourceId,
        Guid? providerPersonId,
        Guid? locationId,
        AvailableBookingSlotDto slot,
        int quantity,
        int ttlMinutes,
        DateTimeOffset nowUtc
    )
    {
        Span<byte> bytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(bytes);
        var clampedTtl = Math.Clamp(ttlMinutes, 1, 60);

        return new BookingSlotHold(BookingSlotHoldId.Create())
        {
            HoldToken = $"HLD-{Convert.ToHexString(bytes)}",
            TargetKind = targetKind,
            BookableServiceId = serviceId,
            RentableResourceId = resourceId,
            ProviderPersonId = providerPersonId,
            LocationId = locationId,
            SlotStartUtc = slot.SlotStartUtc.ToUniversalTime(),
            SlotEndUtc = slot.SlotEndUtc.ToUniversalTime(),
            EffectiveBlockStartUtc = slot.EffectiveBlockStartUtc.ToUniversalTime(),
            EffectiveBlockEndUtc = slot.EffectiveBlockEndUtc.ToUniversalTime(),
            Quantity = quantity,
            ExpiresAtUtc = nowUtc.ToUniversalTime().AddMinutes(clampedTtl),
        };
    }

    public bool IsActiveAt(DateTimeOffset nowUtc)
    {
        return !IsCompleted && ExpiresAtUtc > nowUtc;
    }

    public bool MatchesCandidate(
        BookingTargetKind targetKind,
        BookableServiceId? serviceId,
        RentableResourceId? resourceId,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        int quantity
    )
    {
        return MatchesTarget(targetKind, serviceId, resourceId)
            && MatchesWindowAndQuantity(slotStartUtc, slotEndUtc, quantity);
    }

    private bool MatchesTarget(
        BookingTargetKind targetKind,
        BookableServiceId? serviceId,
        RentableResourceId? resourceId
    )
    {
        return TargetKind == targetKind
            && BookableServiceId == serviceId
            && RentableResourceId == resourceId;
    }

    private bool MatchesWindowAndQuantity(
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        int quantity
    )
    {
        return SlotStartUtc == slotStartUtc.ToUniversalTime()
            && SlotEndUtc == slotEndUtc.ToUniversalTime()
            && Quantity >= quantity;
    }

    public void MarkCompleted(ServiceBookingId bookingId)
    {
        IsCompleted = true;
        CompletedBookingId = bookingId;
    }

    public bool RepointPerson(Guid retiredPersonId, Guid survivingPersonId)
    {
        if (ProviderPersonId != retiredPersonId)
        {
            return false;
        }

        ProviderPersonId = survivingPersonId;
        return true;
    }
}
