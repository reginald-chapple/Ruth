using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class CreateBookableServiceCommandHandler(
    CommerceDbContext dbContext,
    BookingTargetValidator validator
) : ICommandHandler<CreateBookableServiceCommand, BookableServiceResponse>
{
    public async Task<Result<BookableServiceResponse>> HandleAsync(
        CreateBookableServiceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var created = BookableService.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<BookableServiceResponse>(created.Errors);
        }

        var valid = await validator.ValidateServiceAsync(
            command.Spec,
            currentId: null,
            cancellationToken
        );
        if (valid.IsFailure)
        {
            return Result.Failure<BookableServiceResponse>(valid.Errors);
        }

        _ = dbContext.BookableServices.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BookingsMapper.MapService(created.Value);
    }
}
