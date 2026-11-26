using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class CreateBookingSlotHoldCommandHandler(
    CommerceDbContext dbContext,
    BookingScheduleAndConflictLoader loader,
    IClock clock
) : ICommandHandler<CreateBookingSlotHoldCommand, BookingSlotHoldResponse>
{
    public async Task<Result<BookingSlotHoldResponse>> HandleAsync(
        CreateBookingSlotHoldCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = clock.UtcNow;
        var startUtc = command.SlotStartUtc.ToUniversalTime();
        var ctxRes = await loader.BuildSlotContextAsync(
            command.TargetKind,
            command.TargetIdOrSlug,
            command.RequestedDurationMinutes,
            command.Quantity,
            command.ProviderPersonId,
            startUtc.AddHours(-12),
            startUtc.AddHours(36),
            excludeHoldToken: null,
            nowUtc,
            cancellationToken
        );
        if (ctxRes.IsFailure)
        {
            return Result.Failure<BookingSlotHoldResponse>(ctxRes.Errors);
        }

        var ctx = ctxRes.Value;
        var slot = BookingSlotEngine.ValidateExactSlot(
            ctx.Parameters,
            startUtc,
            ctx.OpenWindows,
            ctx.BlackoutWindows,
            ctx.OccupiedBlocks,
            nowUtc
        );
        if (slot is null)
        {
            return CommerceErrors.SlotUnavailable;
        }

        var hold = BookingSlotHold.Create(
            command.TargetKind,
            ctx.Service?.Id,
            ctx.Resource?.Id,
            slot.ProviderPersonId,
            slot.LocationId,
            slot,
            command.Quantity,
            command.HoldTtlMinutes,
            nowUtc
        );

        _ = dbContext.BookingSlotHolds.Add(hold);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BookingsMapper.MapHold(hold);
    }
}
