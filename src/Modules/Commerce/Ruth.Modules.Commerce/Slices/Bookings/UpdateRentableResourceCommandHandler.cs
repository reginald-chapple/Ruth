using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class UpdateRentableResourceCommandHandler(
    CommerceDbContext dbContext,
    BookingTargetValidator validator
) : ICommandHandler<UpdateRentableResourceCommand, RentableResourceResponse>
{
    public async Task<Result<RentableResourceResponse>> HandleAsync(
        UpdateRentableResourceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = RentableResourceId.From(command.ResourceId);
        var entity = await dbContext.RentableResources.FirstOrDefaultAsync(
            r => r.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CommerceErrors.RentableResourceNotFound;
        }

        var valid = await validator.ValidateResourceAsync(command.Spec, id, cancellationToken);
        if (valid.IsFailure)
        {
            return Result.Failure<RentableResourceResponse>(valid.Errors);
        }

        var updated = entity.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<RentableResourceResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BookingsMapper.MapResource(entity);
    }
}
