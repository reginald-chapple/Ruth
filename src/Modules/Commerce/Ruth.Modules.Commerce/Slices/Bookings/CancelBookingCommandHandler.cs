using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class CancelBookingCommandHandler(
    CommerceDbContext dbContext,
    BookingSettlementCollaborator settlementCollaborator,
    WalletPassCommerceSyncCollaborator walletPassSync,
    IClock clock
) : ICommandHandler<CancelBookingCommand, ServiceBookingResponse>
{
    public async Task<Result<ServiceBookingResponse>> HandleAsync(
        CancelBookingCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = ServiceBookingId.From(command.BookingId);
        var booking = await dbContext.ServiceBookings.FirstOrDefaultAsync(
            b => b.Id == id,
            cancellationToken
        );
        if (booking is null)
        {
            return CommerceErrors.BookingNotFound;
        }

        var noticeMinutes = await LoadCancellationNoticeMinutesAsync(booking, cancellationToken);
        var nowUtc = clock.UtcNow;
        var cancelled = booking.Cancel(
            noticeMinutes,
            command.StaffOverride,
            command.Reason,
            nowUtc
        );
        if (cancelled.IsFailure)
        {
            return Result.Failure<ServiceBookingResponse>(cancelled.Errors);
        }

        if (command.IssueRefundOrVoidInvoice)
        {
            var finRes = await settlementCollaborator.RefundOrVoidOnCancelAsync(
                booking,
                command.Reason,
                command.ProcessedByPersonId,
                cancellationToken
            );
            if (finRes.IsFailure)
            {
                return Result.Failure<ServiceBookingResponse>(finRes.Errors);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        if (booking.CustomerPersonId is { } custId)
        {
            await walletPassSync.SyncUpcomingBookingForPersonAsync(
                custId,
                nowUtc,
                cancellationToken
            );
        }

        return BookingsMapper.MapBooking(booking);
    }

    private async Task<int> LoadCancellationNoticeMinutesAsync(
        ServiceOrResourceBooking booking,
        CancellationToken cancellationToken
    )
    {
        return booking.BookableServiceId is { } sId
                ? await dbContext
                    .BookableServices.AsNoTracking()
                    .Where(s => s.Id == sId)
                    .Select(static s => s.CancellationNoticeMinutes)
                    .FirstOrDefaultAsync(cancellationToken)
            : booking.RentableResourceId is { } rId
                ? await dbContext
                    .RentableResources.AsNoTracking()
                    .Where(r => r.Id == rId)
                    .Select(static r => r.CancellationNoticeMinutes)
                    .FirstOrDefaultAsync(cancellationToken)
            : 0;
    }
}
