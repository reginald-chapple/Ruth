namespace Ruth.Modules.Commerce.Contracts.Bookings;

/// <summary>Summary returned after expiring stale TTL booking slot holds.</summary>
/// <param name="ExpiredHoldCount">Number of expired holds released.</param>
public sealed record BookingSlotHoldCleanupResponse(int ExpiredHoldCount);
