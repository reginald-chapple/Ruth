using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class CancelRegistrantCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    WaitlistPromotionService waitlistPromotion
) : ICommandHandler<CancelRegistrantCommand, CancelRegistrantResultResponse>
{
    public async Task<Result<CancelRegistrantResultResponse>> HandleAsync(
        CancelRegistrantCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var regId = RegistrationId.From(command.RegistrationId);
        var registration = await dbContext
            .Registrations.Include(static r => r.Registrants)
                .ThenInclude(static reg => reg.FeeSelections)
            .Include(static r => r.Payments)
            .FirstOrDefaultAsync(r => r.Id == regId, cancellationToken);
        if (registration is null)
        {
            return EventsRegistrationErrors.RegistrationNotFound;
        }

        var cancelRes = registration.CancelRegistrant(
            RegistrationRegistrantId.From(command.RegistrantId)
        );
        if (cancelRes.IsFailure)
        {
            return Result.Failure<CancelRegistrantResultResponse>(cancelRes.Errors);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        IReadOnlyList<Guid> promotedIds = [];
        if (cancelRes.Value)
        {
            var instance = await dbContext.RegistrationInstances.FirstOrDefaultAsync(
                i => i.Id == registration.InstanceId,
                cancellationToken
            );
            var template = instance is not null
                ? await loader.LoadTemplateAsync(
                    instance.TemplateId,
                    asNoTracking: true,
                    cancellationToken
                )
                : null;
            if (instance is not null && template is not null)
            {
                promotedIds = await waitlistPromotion.PromoteAvailableSeatsAsync(
                    instance,
                    template,
                    cancellationToken
                );
                if (promotedIds.Count > 0)
                {
                    _ = await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
        }

        return new CancelRegistrantResultResponse(
            RegistrationMapper.MapRegistration(registration),
            promotedIds
        );
    }
}
