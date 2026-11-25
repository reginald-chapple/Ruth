using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Core.Slices.Locations;

internal sealed class VerifyLocationAddressCommandHandler(
    CoreDbContext dbContext,
    IEnumerable<IAddressVerificationProvider> providers,
    IClock clock
) : ICommandHandler<VerifyLocationAddressCommand, LocationResponse>
{
    public async Task<Result<LocationResponse>> HandleAsync(
        VerifyLocationAddressCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = LocationId.From(command.LocationId);
        var location = await dbContext.Locations.FirstOrDefaultAsync(
            l => l.Id == id,
            cancellationToken
        );
        if (location is null)
        {
            return CoreLocationErrors.LocationNotFound;
        }

        if (location.IsGeoPointLocked)
        {
            return Result.Success(LocationMapper.ToResponse(location));
        }

        var orderedProviders = providers.OrderBy(static p => p.Order).ToList();
        var needsStandardize = command.ReVerify || !location.StandardizeAttemptedOnUtc.HasValue;
        var needsGeocode = command.ReVerify || !location.GeocodeAttemptedOnUtc.HasValue;
        var nowUtc = clock.UtcNow;

        foreach (var provider in orderedProviders)
        {
            var canStandardize = needsStandardize && provider.SupportsStandardization;
            var canGeocode = needsGeocode && provider.SupportsGeocoding;
            if (!canStandardize && !canGeocode)
            {
                continue;
            }

            var currentAddress = new LocationAddressDto(
                location.Street1,
                location.Street2,
                location.City,
                location.County,
                location.State,
                location.PostalCode,
                location.Country
            );
            var outcome = await provider.VerifyAsync(currentAddress, cancellationToken);
            location.ApplyVerificationOutcome(outcome, nowUtc, canStandardize, canGeocode);

            if (outcome.Status.HasFlag(AddressVerificationStatus.Standardized))
            {
                needsStandardize = false;
            }

            if (outcome.Status.HasFlag(AddressVerificationStatus.Geocoded))
            {
                needsGeocode = false;
            }

            if (!needsStandardize && !needsGeocode)
            {
                break;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(LocationMapper.ToResponse(location));
    }
}
