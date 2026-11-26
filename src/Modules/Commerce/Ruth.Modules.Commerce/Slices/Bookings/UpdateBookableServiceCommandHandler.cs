using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class UpdateBookableServiceCommandHandler(
    CommerceDbContext dbContext,
    BookingTargetValidator validator
) : ICommandHandler<UpdateBookableServiceCommand, BookableServiceResponse>
{
    public async Task<Result<BookableServiceResponse>> HandleAsync(
        UpdateBookableServiceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);
        var id = BookableServiceId.From(command.ServiceId);
        var entity = await dbContext.BookableServices.FirstOrDefaultAsync(
            s => s.Id == id,
            cancellationToken
        );
        if (entity is null)
        {
            return CommerceErrors.BookableServiceNotFound;
        }

        var valid = await validator.ValidateServiceAsync(command.Spec, id, cancellationToken);
        if (valid.IsFailure)
        {
            return Result.Failure<BookableServiceResponse>(valid.Errors);
        }

        var updated = entity.Update(command.Spec);
        if (updated.IsFailure)
        {
            return Result.Failure<BookableServiceResponse>(updated.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BookingsMapper.MapService(entity);
    }
}
