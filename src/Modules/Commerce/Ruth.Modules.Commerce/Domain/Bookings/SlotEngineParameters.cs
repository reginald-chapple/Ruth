using Ruth.Modules.Commerce.Contracts.Bookings;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>Slot engine parameters unifying service and resource scheduling rules.</summary>
public sealed record SlotEngineParameters(
    BookingTargetKind TargetKind,
    BookableServiceId? BookableServiceId,
    RentableResourceId? RentableResourceId,
    Guid? DefaultLocationId,
    int DurationMinutes,
    int SlotIncrementMinutes,
    int SetupBufferMinutes,
    int TeardownBufferMinutes,
    int CapacityPerSlot,
    int MinimumLeadTimeMinutes,
    int MaximumAdvanceDays,
    int RequestedQuantity,
    Guid? RequestedProviderPersonId,
    IReadOnlyList<Guid> CandidateProviderIds
);
