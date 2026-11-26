using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class BookingTargetValidator(
    CommerceDbContext dbContext,
    IQueryDispatcher queryDispatcher
)
{
    public async Task<Result> ValidateServiceAsync(
        BookableServiceSpec spec,
        BookableServiceId? currentId,
        CancellationToken cancellationToken
    )
    {
        if (spec.RevenueAccountId is { } accId && accId != Guid.Empty)
        {
            var accRes = await queryDispatcher.QueryAsync<
                GetFinancialAccountByIdQuery,
                FinancialAccountResponse
            >(new GetFinancialAccountByIdQuery(accId), cancellationToken);
            if (accRes.IsFailure || !accRes.Value.IsActive)
            {
                return CommerceErrors.BookableServiceInvalid;
            }
        }

        if (spec.DefaultLocationId is { } locId && locId != Guid.Empty)
        {
            var locRes = await queryDispatcher.QueryAsync<GetLocationByIdQuery, LocationResponse>(
                new GetLocationByIdQuery(locId),
                cancellationToken
            );
            if (locRes.IsFailure || !locRes.Value.IsActive)
            {
                return CommerceErrors.BookableServiceInvalid;
            }
        }

        var slug = spec.Slug.Trim().ToLowerInvariant();
        var conflict = await dbContext.BookableServices.AnyAsync(
            s => s.Slug == slug && (!currentId.HasValue || s.Id != currentId.Value),
            cancellationToken
        );
        return conflict ? CommerceErrors.SlugConflict : Result.Success();
    }

    public async Task<Result> ValidateResourceAsync(
        RentableResourceSpec spec,
        RentableResourceId? currentId,
        CancellationToken cancellationToken
    )
    {
        var locRes = await queryDispatcher.QueryAsync<GetLocationByIdQuery, LocationResponse>(
            new GetLocationByIdQuery(spec.CoreLocationId),
            cancellationToken
        );
        if (locRes.IsFailure || !locRes.Value.IsActive)
        {
            return CommerceErrors.RentableResourceInvalid;
        }

        if (spec.RevenueAccountId is { } accId && accId != Guid.Empty)
        {
            var accRes = await queryDispatcher.QueryAsync<
                GetFinancialAccountByIdQuery,
                FinancialAccountResponse
            >(new GetFinancialAccountByIdQuery(accId), cancellationToken);
            if (accRes.IsFailure || !accRes.Value.IsActive)
            {
                return CommerceErrors.RentableResourceInvalid;
            }
        }

        var slug = spec.Slug.Trim().ToLowerInvariant();
        var conflict = await dbContext.RentableResources.AnyAsync(
            r => r.Slug == slug && (!currentId.HasValue || r.Id != currentId.Value),
            cancellationToken
        );
        return conflict ? CommerceErrors.SlugConflict : Result.Success();
    }
}
