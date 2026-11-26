using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class PreviewBookingQuoteQueryHandler(
    BookingScheduleAndConflictLoader loader,
    MemberEligibilityResolver eligibilityResolver
) : IQueryHandler<PreviewBookingQuoteQuery, BookingQuoteResponse>
{
    public async Task<Result<BookingQuoteResponse>> HandleAsync(
        PreviewBookingQuoteQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.Quantity <= 0)
        {
            return CommerceErrors.BookingPaymentInvalid;
        }

        var profile = await eligibilityResolver.ResolveAsync(
            query.CustomerPersonId,
            query.ExplicitMembershipTier,
            cancellationToken
        );

        if (query.TargetKind == BookingTargetKind.Service)
        {
            var service = await loader.LoadServiceAsync(
                query.TargetIdOrSlug,
                asNoTracking: true,
                cancellationToken
            );
            return service is null
                ? CommerceErrors.BookableServiceNotFound
                : BookingPricingCalculator.EvaluateServiceQuote(
                    service,
                    query.Quantity,
                    profile.Tiers,
                    profile.GroupIds,
                    query.RedeemPunchPassId is { } p && p != Guid.Empty
                );
        }

        var resource = await loader.LoadResourceAsync(
            query.TargetIdOrSlug,
            asNoTracking: true,
            cancellationToken
        );
        if (resource is null)
        {
            return CommerceErrors.RentableResourceNotFound;
        }

        var duration = query.RequestedDurationMinutes ?? resource.MinDurationMinutes;
        return duration < resource.MinDurationMinutes || duration > resource.MaxDurationMinutes
            ? CommerceErrors.SlotUnavailable
            : BookingPricingCalculator.EvaluateResourceQuote(
                resource,
                duration,
                query.Quantity,
                profile.Tiers,
                profile.GroupIds
            );
    }
}
