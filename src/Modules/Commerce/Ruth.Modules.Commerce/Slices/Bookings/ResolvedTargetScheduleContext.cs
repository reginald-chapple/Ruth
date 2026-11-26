using Ruth.Modules.Commerce.Domain.Bookings;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed record ResolvedTargetScheduleContext(
    BookableService? Service,
    RentableResource? Resource,
    SlotEngineParameters Parameters,
    IReadOnlyList<NormalizedAvailabilityInterval> OpenWindows,
    IReadOnlyList<NormalizedAvailabilityInterval> BlackoutWindows,
    IReadOnlyList<OccupiedBookingBlock> OccupiedBlocks
);
