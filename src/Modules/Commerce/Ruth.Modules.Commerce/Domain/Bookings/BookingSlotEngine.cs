using Ruth.Modules.Commerce.Contracts.Bookings;

namespace Ruth.Modules.Commerce.Domain.Bookings;

/// <summary>High-performance conflict-free slot engine enforcing lead/horizon bounds, setup/teardown buffers, blackouts, and TTL holds.</summary>
public static class BookingSlotEngine
{
    public static IReadOnlyList<AvailableBookingSlotDto> ComputeAvailableSlots(
        SlotEngineParameters parameters,
        IReadOnlyList<NormalizedAvailabilityInterval> openWindows,
        IReadOnlyList<NormalizedAvailabilityInterval> blackoutWindows,
        IReadOnlyList<OccupiedBookingBlock> occupiedBlocks,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        DateTimeOffset nowUtc
    )
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(openWindows);
        ArgumentNullException.ThrowIfNull(blackoutWindows);
        ArgumentNullException.ThrowIfNull(occupiedBlocks);

        if (
            parameters.RequestedQuantity <= 0
            || parameters.RequestedQuantity > parameters.CapacityPerSlot
            || rangeEndUtc <= rangeStartUtc
        )
        {
            return [];
        }

        var earliestStart = MaxUtc(
            rangeStartUtc.ToUniversalTime(),
            nowUtc.ToUniversalTime().AddMinutes(parameters.MinimumLeadTimeMinutes)
        );
        var latestStart = MinUtc(
            rangeEndUtc.ToUniversalTime(),
            nowUtc.ToUniversalTime().AddDays(parameters.MaximumAdvanceDays)
        );
        if (latestStart <= earliestStart)
        {
            return [];
        }

        var results = new List<AvailableBookingSlotDto>();
        var seen = new HashSet<(DateTimeOffset Start, Guid? Provider, Guid? Location)>();

        foreach (var window in openWindows)
        {
            ExpandWindowCandidates(
                parameters,
                window,
                blackoutWindows,
                occupiedBlocks,
                earliestStart,
                latestStart,
                seen,
                results
            );
        }

        results.Sort(static (a, b) => a.SlotStartUtc.CompareTo(b.SlotStartUtc));
        return results;
    }

    public static AvailableBookingSlotDto? ValidateExactSlot(
        SlotEngineParameters parameters,
        DateTimeOffset requestedSlotStartUtc,
        IReadOnlyList<NormalizedAvailabilityInterval> openWindows,
        IReadOnlyList<NormalizedAvailabilityInterval> blackoutWindows,
        IReadOnlyList<OccupiedBookingBlock> occupiedBlocks,
        DateTimeOffset nowUtc
    )
    {
        var start = requestedSlotStartUtc.ToUniversalTime();
        var end = start.AddMinutes(parameters.DurationMinutes);
        var slots = ComputeAvailableSlots(
            parameters,
            openWindows,
            blackoutWindows,
            occupiedBlocks,
            start,
            end,
            nowUtc
        );

        return slots.FirstOrDefault(s =>
            s.SlotStartUtc == start
            && (
                !parameters.RequestedProviderPersonId.HasValue
                || s.ProviderPersonId == parameters.RequestedProviderPersonId
            )
        );
    }

    private static void ExpandWindowCandidates(
        SlotEngineParameters parameters,
        NormalizedAvailabilityInterval window,
        IReadOnlyList<NormalizedAvailabilityInterval> blackouts,
        IReadOnlyList<OccupiedBookingBlock> occupied,
        DateTimeOffset earliestStart,
        DateTimeOffset latestStart,
        HashSet<(DateTimeOffset, Guid?, Guid?)> seen,
        List<AvailableBookingSlotDto> results
    )
    {
        var providers = ResolveWindowProviders(parameters, window.ProviderPersonId);
        if (providers.Count == 0)
        {
            return;
        }

        var locationId = window.LocationId ?? parameters.DefaultLocationId;
        var duration = TimeSpan.FromMinutes(parameters.DurationMinutes);
        var step = TimeSpan.FromMinutes(parameters.SlotIncrementMinutes);

        for (
            var cursor = window.StartUtc;
            cursor + duration <= window.EndUtc;
            cursor = cursor.Add(step)
        )
        {
            if (cursor >= earliestStart && cursor <= latestStart)
            {
                EvaluateCandidateAtCursor(
                    parameters,
                    cursor,
                    providers,
                    locationId,
                    blackouts,
                    occupied,
                    seen,
                    results
                );
            }
        }
    }

    private static void EvaluateCandidateAtCursor(
        SlotEngineParameters parameters,
        DateTimeOffset cursor,
        IReadOnlyList<Guid?> providers,
        Guid? locationId,
        IReadOnlyList<NormalizedAvailabilityInterval> blackouts,
        IReadOnlyList<OccupiedBookingBlock> occupied,
        HashSet<(DateTimeOffset, Guid?, Guid?)> seen,
        List<AvailableBookingSlotDto> results
    )
    {
        var slotEnd = cursor.AddMinutes(parameters.DurationMinutes);
        var blockStart = cursor.AddMinutes(-parameters.SetupBufferMinutes);
        var blockEnd = slotEnd.AddMinutes(parameters.TeardownBufferMinutes);

        foreach (var providerId in providers)
        {
            if (
                !seen.Add((cursor, providerId, locationId))
                || IsBlockedByBlackout(blockStart, blockEnd, providerId, locationId, blackouts)
            )
            {
                continue;
            }

            var remaining = CalculateRemainingCapacity(
                parameters,
                cursor,
                slotEnd,
                blockStart,
                blockEnd,
                providerId,
                locationId,
                occupied
            );

            if (remaining >= parameters.RequestedQuantity)
            {
                results.Add(
                    new AvailableBookingSlotDto(
                        cursor,
                        slotEnd,
                        blockStart,
                        blockEnd,
                        providerId,
                        locationId,
                        remaining
                    )
                );
            }
        }
    }

    private static IReadOnlyList<Guid?> ResolveWindowProviders(
        SlotEngineParameters parameters,
        Guid? windowProviderId
    )
    {
        return parameters.TargetKind == BookingTargetKind.Resource ? [null]
            : parameters.RequestedProviderPersonId is { } reqId
                ? windowProviderId is null || windowProviderId == reqId ? [reqId]
                    : []
            : windowProviderId.HasValue ? [windowProviderId]
            : parameters.CandidateProviderIds.Count > 0
                ? [.. parameters.CandidateProviderIds.Cast<Guid?>()]
            : [null];
    }

    private static bool IsBlockedByBlackout(
        DateTimeOffset blockStart,
        DateTimeOffset blockEnd,
        Guid? providerId,
        Guid? locationId,
        IReadOnlyList<NormalizedAvailabilityInterval> blackouts
    )
    {
        foreach (var b in blackouts)
        {
            if (BlackoutOverlapsCandidate(b, blockStart, blockEnd, providerId, locationId))
            {
                return true;
            }
        }

        return false;
    }

    private static bool BlackoutOverlapsCandidate(
        NormalizedAvailabilityInterval blackout,
        DateTimeOffset blockStart,
        DateTimeOffset blockEnd,
        Guid? providerId,
        Guid? locationId
    )
    {
        var timeOverlaps = blockStart < blackout.EndUtc && blackout.StartUtc < blockEnd;
        var providerApplies =
            !blackout.ProviderPersonId.HasValue || blackout.ProviderPersonId == providerId;
        var locationApplies = !blackout.LocationId.HasValue || blackout.LocationId == locationId;
        return timeOverlaps && providerApplies && locationApplies;
    }

    private static int CalculateRemainingCapacity(
        SlotEngineParameters parameters,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        DateTimeOffset blockStart,
        DateTimeOffset blockEnd,
        Guid? providerId,
        Guid? locationId,
        IReadOnlyList<OccupiedBookingBlock> occupied
    )
    {
        var consumed = 0;
        foreach (var occ in occupied)
        {
            if (blockStart >= occ.EffectiveBlockEndUtc || occ.EffectiveBlockStartUtc >= blockEnd)
            {
                continue;
            }

            if (
                IsExclusiveCrossConflict(
                    parameters,
                    slotStart,
                    slotEnd,
                    providerId,
                    locationId,
                    occ
                )
            )
            {
                return 0;
            }

            if (ConsumesTargetCapacity(parameters, providerId, occ))
            {
                consumed += occ.Quantity;
            }
        }

        return Math.Max(0, parameters.CapacityPerSlot - consumed);
    }

    private static bool IsExclusiveCrossConflict(
        SlotEngineParameters parameters,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        Guid? providerId,
        Guid? locationId,
        OccupiedBookingBlock occ
    )
    {
        var sameSharedSlot = IsSameTargetAndExactSlot(parameters, slotStart, slotEnd, occ);
        return HasProviderConflict(parameters, providerId, sameSharedSlot, occ)
            || HasLocationConflict(parameters, locationId, sameSharedSlot, occ);
    }

    private static bool IsSameTargetAndExactSlot(
        SlotEngineParameters parameters,
        DateTimeOffset slotStart,
        DateTimeOffset slotEnd,
        OccupiedBookingBlock occ
    )
    {
        var sameTarget =
            occ.TargetKind == parameters.TargetKind
            && EqualityComparer<BookableServiceId?>.Default.Equals(
                occ.BookableServiceId,
                parameters.BookableServiceId
            )
            && EqualityComparer<RentableResourceId?>.Default.Equals(
                occ.RentableResourceId,
                parameters.RentableResourceId
            );
        return sameTarget && occ.SlotStartUtc == slotStart && occ.SlotEndUtc == slotEnd;
    }

    private static bool HasProviderConflict(
        SlotEngineParameters parameters,
        Guid? providerId,
        bool sameSharedSlot,
        OccupiedBookingBlock occ
    )
    {
        return providerId.HasValue
            && occ.ProviderPersonId == providerId
            && (!sameSharedSlot || parameters.CapacityPerSlot == 1);
    }

    private static bool HasLocationConflict(
        SlotEngineParameters parameters,
        Guid? locationId,
        bool sameSharedSlot,
        OccupiedBookingBlock occ
    )
    {
        var exclusiveRoom =
            parameters.TargetKind == BookingTargetKind.Service || parameters.CapacityPerSlot == 1;
        return locationId.HasValue
            && occ.LocationId == locationId
            && !sameSharedSlot
            && exclusiveRoom;
    }

    private static bool ConsumesTargetCapacity(
        SlotEngineParameters parameters,
        Guid? providerId,
        OccupiedBookingBlock occ
    )
    {
        var sameProvider =
            !providerId.HasValue
            || EqualityComparer<Guid?>.Default.Equals(occ.ProviderPersonId, providerId);
        return parameters.TargetKind == BookingTargetKind.Resource
            ? EqualityComparer<RentableResourceId?>.Default.Equals(
                occ.RentableResourceId,
                parameters.RentableResourceId
            )
            : EqualityComparer<BookableServiceId?>.Default.Equals(
                occ.BookableServiceId,
                parameters.BookableServiceId
            ) && sameProvider;
    }

    private static DateTimeOffset MaxUtc(DateTimeOffset a, DateTimeOffset b)
    {
        return a >= b ? a : b;
    }

    private static DateTimeOffset MinUtc(DateTimeOffset a, DateTimeOffset b)
    {
        return a <= b ? a : b;
    }
}
