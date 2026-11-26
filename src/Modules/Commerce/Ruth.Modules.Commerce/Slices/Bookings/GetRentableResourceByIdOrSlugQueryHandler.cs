using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class GetRentableResourceByIdOrSlugQueryHandler(
    BookingScheduleAndConflictLoader loader
) : IQueryHandler<GetRentableResourceByIdOrSlugQuery, RentableResourceResponse>
{
    public async Task<Result<RentableResourceResponse>> HandleAsync(
        GetRentableResourceByIdOrSlugQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var resource = await loader.LoadResourceAsync(
            query.IdOrSlug,
            asNoTracking: true,
            cancellationToken
        );
        return resource is null
            ? CommerceErrors.RentableResourceNotFound
            : BookingsMapper.MapResource(resource);
    }
}
