using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.SharedKernel.Events;

namespace Ruth.Modules.Commerce.Slices.Events;

internal sealed class CommercePersonMergedConsumer(CommerceDbContext dbContext)
    : IIntegrationEventConsumer<PersonMergedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PersonMergedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var retired = integrationEvent.RetiredPersonId;
        var surviving = integrationEvent.SurvivingPersonId;
        if (retired == Guid.Empty || surviving == Guid.Empty || retired == surviving)
        {
            return;
        }

        var changed = false;
        changed |= await RepointOrdersAndPassesAsync(retired, surviving, cancellationToken);
        changed |= await RepointBookingsAndRulesAsync(retired, surviving, cancellationToken);

        if (changed)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<bool> RepointOrdersAndPassesAsync(
        Guid retired,
        Guid surviving,
        CancellationToken cancellationToken
    )
    {
        var modified = false;
        var orders = await dbContext
            .Orders.Where(o => o.BuyerPersonId == retired)
            .ToListAsync(cancellationToken);
        foreach (var order in orders)
        {
            modified |= order.RepointPerson(retired, surviving);
        }

        var passes = await dbContext
            .IssuedPunchPasses.Include(static p => p.LedgerEntries)
            .Where(p =>
                p.OwnerPersonId == retired
                || p.LedgerEntries.Any(e => e.RedeemedForPersonId == retired)
            )
            .ToListAsync(cancellationToken);
        foreach (var pass in passes)
        {
            modified |= pass.RepointPerson(retired, surviving);
        }

        return modified;
    }

    private async Task<bool> RepointBookingsAndRulesAsync(
        Guid retired,
        Guid surviving,
        CancellationToken cancellationToken
    )
    {
        var modified = false;
        var services = await dbContext.BookableServices.ToListAsync(cancellationToken);
        foreach (var svc in services)
        {
            modified |= svc.RepointPerson(retired, surviving);
        }

        var rules = await dbContext
            .AvailabilityRules.Where(r => r.ProviderPersonId == retired)
            .ToListAsync(cancellationToken);
        foreach (var rule in rules)
        {
            modified |= rule.RepointPerson(retired, surviving);
        }

        var holds = await dbContext
            .BookingSlotHolds.Where(h => h.ProviderPersonId == retired)
            .ToListAsync(cancellationToken);
        foreach (var hold in holds)
        {
            modified |= hold.RepointPerson(retired, surviving);
        }

        var bookings = await dbContext
            .ServiceBookings.Where(b =>
                b.CustomerPersonId == retired || b.ProviderPersonId == retired
            )
            .ToListAsync(cancellationToken);
        foreach (var booking in bookings)
        {
            modified |= booking.RepointPerson(retired, surviving);
        }

        return modified;
    }
}
