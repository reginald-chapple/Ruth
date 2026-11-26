using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class ExpireRegistrationSessionsCommandHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    WaitlistPromotionService waitlistPromotion,
    TimeProvider timeProvider
) : ICommandHandler<ExpireRegistrationSessionsCommand, RegistrationSessionCleanupResultResponse>
{
    public async Task<Result<RegistrationSessionCleanupResultResponse>> HandleAsync(
        ExpireRegistrationSessionsCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var nowUtc = timeProvider.GetUtcNow();
        var staleSessions = await dbContext
            .RegistrationSessions.Where(s =>
                s.Status == RegistrationSessionStatus.Active && s.ExpiresAtUtc <= nowUtc
            )
            .ToListAsync(cancellationToken);

        if (staleSessions.Count == 0)
        {
            return new RegistrationSessionCleanupResultResponse(0, 0);
        }

        var affectedInstanceIds = staleSessions
            .Where(static s => s.HeldConfirmedSeats > 0)
            .Select(static s => s.InstanceId)
            .Distinct()
            .ToArray();

        var expiredCount = 0;
        foreach (var session in staleSessions)
        {
            if (session.MarkExpiredIfPastDue(nowUtc))
            {
                expiredCount++;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        var promotedTotal = await PromoteFreedSeatsAsync(affectedInstanceIds, cancellationToken);
        return new RegistrationSessionCleanupResultResponse(expiredCount, promotedTotal);
    }

    private async Task<int> PromoteFreedSeatsAsync(
        RegistrationInstanceId[] affectedInstanceIds,
        CancellationToken cancellationToken
    )
    {
        var promotedTotal = 0;
        foreach (var instanceId in affectedInstanceIds)
        {
            var instance = await dbContext.RegistrationInstances.FirstOrDefaultAsync(
                i => i.Id == instanceId,
                cancellationToken
            );
            if (instance is null)
            {
                continue;
            }

            var template = await loader.LoadTemplateAsync(
                instance.TemplateId,
                asNoTracking: true,
                cancellationToken
            );
            if (template is not null)
            {
                var promoted = await waitlistPromotion.PromoteAvailableSeatsAsync(
                    instance,
                    template,
                    cancellationToken
                );
                promotedTotal += promoted.Count;
            }
        }

        if (promotedTotal > 0)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }

        return promotedTotal;
    }
}
