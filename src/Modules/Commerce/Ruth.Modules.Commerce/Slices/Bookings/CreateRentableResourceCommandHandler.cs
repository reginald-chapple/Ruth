using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class CreateRentableResourceCommandHandler(
    CommerceDbContext dbContext,
    BookingTargetValidator validator
) : ICommandHandler<CreateRentableResourceCommand, RentableResourceResponse>
{
    public async Task<Result<RentableResourceResponse>> HandleAsync(
        CreateRentableResourceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = RentableResource.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<RentableResourceResponse>(created.Errors);
        }

        var valid = await validator.ValidateResourceAsync(
            command.Spec,
            currentId: null,
            cancellationToken
        );
        if (valid.IsFailure)
        {
            return Result.Failure<RentableResourceResponse>(valid.Errors);
        }

        _ = dbContext.RentableResources.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BookingsMapper.MapResource(created.Value);
    }
}
