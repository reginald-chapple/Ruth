using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetBookableServiceByIdOrSlugQueryHandler(
    BookingScheduleAndConflictLoader loader
) : IQueryHandler<GetBookableServiceByIdOrSlugQuery, BookableServiceResponse>
{
    public async Task<Result<BookableServiceResponse>> HandleAsync(
        GetBookableServiceByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var service = await loader.LoadServiceAsync(
            query.IdOrSlug,
            asNoTracking: true,
            cancellationToken
        );
        return service is null
            ? CommerceErrors.BookableServiceNotFound
            : BookingsMapper.MapService(service);
    }
}
