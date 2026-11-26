using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class ExpireBookingSlotHoldsCommandHandler(
    CommerceDbContext dbContext,
    IClock clock
) : ICommandHandler<ExpireBookingSlotHoldsCommand, BookingSlotHoldCleanupResponse>
{
    public async Task<Result<BookingSlotHoldCleanupResponse>> HandleAsync(
        ExpireBookingSlotHoldsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var nowUtc = clock.UtcNow;
        var stale = await dbContext
            .BookingSlotHolds.Where(h => !h.IsCompleted && h.ExpiresAtUtc <= nowUtc)
            .ToListAsync(cancellationToken);

        if (stale.Count > 0)
        {
            dbContext.BookingSlotHolds.RemoveRange(stale);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new BookingSlotHoldCleanupResponse(stale.Count);
    }
}
