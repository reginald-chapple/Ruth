using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class QueryAvailableBookingSlotsQueryHandler(
    BookingScheduleAndConflictLoader loader,
    IClock clock
) : IQueryHandler<QueryAvailableBookingSlotsQuery, IReadOnlyList<AvailableBookingSlotDto>>
{
    public async Task<Result<IReadOnlyList<AvailableBookingSlotDto>>> HandleAsync(
        QueryAvailableBookingSlotsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var nowUtc = clock.UtcNow;
        var ctxRes = await loader.BuildSlotContextAsync(
            query.TargetKind,
            query.TargetIdOrSlug,
            query.RequestedDurationMinutes,
            query.RequestedQuantity,
            query.ProviderPersonId,
            query.RangeStartUtc,
            query.RangeEndUtc,
            excludeHoldToken: null,
            nowUtc,
            cancellationToken
        );
        if (ctxRes.IsFailure)
        {
            return Result.Failure<IReadOnlyList<AvailableBookingSlotDto>>(ctxRes.Errors);
        }

        var ctx = ctxRes.Value;
        var slots = BookingSlotEngine.ComputeAvailableSlots(
            ctx.Parameters,
            ctx.OpenWindows,
            ctx.BlackoutWindows,
            ctx.OccupiedBlocks,
            query.RangeStartUtc,
            query.RangeEndUtc,
            nowUtc
        );
        return Result.Success(slots);
    }
}
