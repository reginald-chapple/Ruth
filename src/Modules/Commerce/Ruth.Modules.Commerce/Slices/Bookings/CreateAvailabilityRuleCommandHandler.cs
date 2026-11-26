using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class CreateAvailabilityRuleCommandHandler(CommerceDbContext dbContext)
    : ICommandHandler<CreateAvailabilityRuleCommand, AvailabilityRuleResponse>
{
    public async Task<Result<AvailabilityRuleResponse>> HandleAsync(
        CreateAvailabilityRuleCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var created = AvailabilityRule.Create(command.Spec);
        if (created.IsFailure)
        {
            return Result.Failure<AvailabilityRuleResponse>(created.Errors);
        }

        var targetExists =
            command.Spec.TargetKind == BookingTargetKind.Service
                ? await dbContext.BookableServices.AnyAsync(
                    s => s.Id == created.Value.BookableServiceId!.Value,
                    cancellationToken
                )
                : await dbContext.RentableResources.AnyAsync(
                    r => r.Id == created.Value.RentableResourceId!.Value,
                    cancellationToken
                );
        if (!targetExists)
        {
            return command.Spec.TargetKind == BookingTargetKind.Service
                ? CommerceErrors.BookableServiceNotFound
                : CommerceErrors.RentableResourceNotFound;
        }

        _ = dbContext.AvailabilityRules.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return BookingsMapper.MapRule(created.Value);
    }
}
