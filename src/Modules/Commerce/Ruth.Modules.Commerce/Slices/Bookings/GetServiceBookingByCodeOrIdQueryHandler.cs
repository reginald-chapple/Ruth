using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetServiceBookingByCodeOrIdQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetServiceBookingByCodeOrIdQuery, ServiceBookingResponse>
{
    public async Task<Result<ServiceBookingResponse>> HandleAsync(
        GetServiceBookingByCodeOrIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.CodeOrId))
        {
            return CommerceErrors.BookingNotFound;
        }

        var trimmed = query.CodeOrId.Trim();
        var upperCode = trimmed.ToUpperInvariant();
        var q = dbContext.ServiceBookings.AsNoTracking();
        var booking = Guid.TryParse(trimmed, out var guid)
            ? await q.FirstOrDefaultAsync(
                b => b.Id == ServiceBookingId.From(guid),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(b => b.ConfirmationCode == upperCode, cancellationToken);

        return booking is null
            ? CommerceErrors.BookingNotFound
            : BookingsMapper.MapBooking(booking);
    }
}
