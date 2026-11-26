using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetServiceBookingsQueryHandler(CommerceDbContext dbContext)
    : IQueryHandler<GetServiceBookingsQuery, IReadOnlyList<ServiceBookingResponse>>
{
    public async Task<Result<IReadOnlyList<ServiceBookingResponse>>> HandleAsync(
        GetServiceBookingsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var q = ApplyFilters(dbContext.ServiceBookings.AsNoTracking(), query);
        var items = await q.OrderBy(static b => b.StartUtc).ToListAsync(cancellationToken);
        return items.Select(BookingsMapper.MapBooking).ToArray();
    }

    private static IQueryable<ServiceOrResourceBooking> ApplyFilters(
        IQueryable<ServiceOrResourceBooking> q,
        GetServiceBookingsQuery query
    )
    {
        if (query.RangeStartUtc.HasValue)
        {
            var start = query.RangeStartUtc.Value.ToUniversalTime();
            q = q.Where(b => b.EndUtc >= start);
        }

        if (query.RangeEndUtc.HasValue)
        {
            var end = query.RangeEndUtc.Value.ToUniversalTime();
            q = q.Where(b => b.StartUtc <= end);
        }

        if (query.BookableServiceId is { } sId && sId != Guid.Empty)
        {
            var typed = BookableServiceId.From(sId);
            q = q.Where(b => b.BookableServiceId == typed);
        }

        if (query.RentableResourceId is { } rId && rId != Guid.Empty)
        {
            var typed = RentableResourceId.From(rId);
            q = q.Where(b => b.RentableResourceId == typed);
        }

        if (query.ProviderPersonId is { } pId && pId != Guid.Empty)
        {
            q = q.Where(b => b.ProviderPersonId == pId);
        }

        if (query.CustomerPersonId is { } cId && cId != Guid.Empty)
        {
            q = q.Where(b => b.CustomerPersonId == cId);
        }

        if (query.Status.HasValue)
        {
            var status = query.Status.Value;
            q = q.Where(b => b.Status == status);
        }

        return q;
    }
}
