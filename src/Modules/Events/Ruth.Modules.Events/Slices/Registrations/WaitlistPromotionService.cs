using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Events;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class WaitlistPromotionService(
    EventsDbContext dbContext,
    RegistrationInventoryLoader inventoryLoader,
    TimeProvider timeProvider
)
{
    public async Task<IReadOnlyList<Guid>> PromoteAvailableSeatsAsync(
        RegistrationInstance instance,
        RegistrationTemplate template,
        CancellationToken cancellationToken
    )
    {
        var nowUtc = timeProvider.GetUtcNow();
        var (confirmed, activeHeld, waitlisted, _) = await inventoryLoader.LoadSeatSnapshotAsync(
            instance.Id,
            nowUtc,
            excludeSessionToken: null,
            cancellationToken
        );
        if (waitlisted == 0)
        {
            return [];
        }

        var openSeats = instance.MaxAttendees.HasValue
            ? Math.Max(0, instance.MaxAttendees.Value - confirmed - activeHeld)
            : waitlisted;
        if (openSeats <= 0)
        {
            return [];
        }

        var candidates = await dbContext
            .RegistrationRegistrants.Where(r =>
                r.InstanceId == instance.Id && r.Status == RegistrantStatus.Waitlisted
            )
            .OrderBy(static r => r.WaitlistPosition ?? int.MaxValue)
            .ThenBy(static r => r.CreatedOnUtc)
            .Take(openSeats)
            .ToListAsync(cancellationToken);
        if (candidates.Count == 0)
        {
            return [];
        }

        var regIds = candidates.Select(static c => c.RegistrationId).Distinct().ToArray();
        var registrations = await dbContext
            .Registrations.Include(static r => r.Registrants)
                .ThenInclude(static reg => reg.FeeSelections)
            .Include(static r => r.Payments)
            .Where(r => regIds.Contains(r.Id))
            .ToDictionaryAsync(static r => r.Id, cancellationToken);

        return ApplyPromotions(
            candidates,
            registrations,
            instance,
            instance.EffectiveBaseCost(template),
            nowUtc
        );
    }

    private List<Guid> ApplyPromotions(
        List<RegistrationRegistrant> candidates,
        Dictionary<RegistrationId, Registration> registrations,
        RegistrationInstance instance,
        decimal effectiveBaseCost,
        DateTimeOffset nowUtc
    )
    {
        var promotedIds = new List<Guid>(candidates.Count);
        foreach (var candidate in candidates)
        {
            if (
                registrations.TryGetValue(candidate.RegistrationId, out var parentReg)
                && parentReg.PromoteRegistrantFromWaitlist(candidate.Id, effectiveBaseCost)
            )
            {
                promotedIds.Add(candidate.Id.Value);
                dbContext.EnqueueIntegrationEvent(
                    new RegistrantPromotedFromWaitlistIntegrationEvent(
                        Guid.CreateVersion7(),
                        nowUtc,
                        parentReg.Id.Value,
                        candidate.Id.Value,
                        instance.Id.Value,
                        candidate.PersonId,
                        candidate.FirstName,
                        candidate.LastName,
                        candidate.Email
                    )
                );
            }
        }

        return promotedIds;
    }
}
