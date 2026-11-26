using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class RegistrationInventoryLoader(EventsDbContext dbContext)
{
    public Task<RegistrationTemplate?> LoadTemplateAsync(
        RegistrationTemplateId templateId,
        bool asNoTracking,
        CancellationToken cancellationToken
    )
    {
        var query = asNoTracking
            ? dbContext.RegistrationTemplates.AsNoTracking()
            : dbContext.RegistrationTemplates.AsQueryable();

        return query
            .Include(static t => t.Forms)
                .ThenInclude(static f => f.Fields)
            .Include(static t => t.Fees)
                .ThenInclude(static fee => fee.Items)
            .Include(static t => t.Discounts)
            .Include(static t => t.GroupPlacements)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }

    public Task<RegistrationInstance?> LoadInstanceByIdOrSlugAsync(
        string idOrSlug,
        bool asNoTracking,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            return Task.FromResult<RegistrationInstance?>(null);
        }

        var query = asNoTracking
            ? dbContext.RegistrationInstances.AsNoTracking()
            : dbContext.RegistrationInstances.AsQueryable();

        if (Guid.TryParse(idOrSlug.Trim(), out var guid))
        {
            var id = RegistrationInstanceId.From(guid);
            return query.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        }

        var slug = idOrSlug.Trim().ToLowerInvariant();
        return query.FirstOrDefaultAsync(i => i.Slug == slug, cancellationToken);
    }

    public async Task<(
        int ConfirmedCount,
        int ActiveHeldSeats,
        int WaitlistedCount,
        int MaxWaitlistPosition
    )> LoadSeatSnapshotAsync(
        RegistrationInstanceId instanceId,
        DateTimeOffset nowUtc,
        string? excludeSessionToken,
        CancellationToken cancellationToken
    )
    {
        var confirmed = await dbContext.RegistrationRegistrants.CountAsync(
            r => r.InstanceId == instanceId && r.Status == RegistrantStatus.Confirmed,
            cancellationToken
        );
        var waitlisted = await dbContext.RegistrationRegistrants.CountAsync(
            r => r.InstanceId == instanceId && r.Status == RegistrantStatus.Waitlisted,
            cancellationToken
        );
        var maxWaitlist = await dbContext
            .RegistrationRegistrants.Where(r =>
                r.InstanceId == instanceId && r.Status == RegistrantStatus.Waitlisted
            )
            .Select(static r => r.WaitlistPosition)
            .MaxAsync(cancellationToken)
            ?? 0;

        var holdsQuery = dbContext.RegistrationSessions.Where(s =>
            s.InstanceId == instanceId
            && s.Status == RegistrationSessionStatus.Active
            && s.ExpiresAtUtc > nowUtc
        );
        if (!string.IsNullOrWhiteSpace(excludeSessionToken))
        {
            var tok = excludeSessionToken.Trim();
            holdsQuery = holdsQuery.Where(s => s.SessionToken != tok);
        }

        var activeHeld = await holdsQuery
            .Select(static s => (int?)s.HeldConfirmedSeats)
            .SumAsync(cancellationToken)
            ?? 0;

        return (confirmed, activeHeld, waitlisted, maxWaitlist);
    }

    public async Task<Dictionary<Guid, int>> LoadConsumedFeeItemQuantitiesAsync(
        RegistrationInstanceId instanceId,
        CancellationToken cancellationToken
    )
    {
        var rows = await dbContext
            .RegistrationRegistrantFeeSelections.Join(
                dbContext.RegistrationRegistrants.Where(r =>
                    r.InstanceId == instanceId && r.Status == RegistrantStatus.Confirmed
                ),
                static s => s.RegistrantId,
                static r => r.Id,
                static (s, _) => new { s.FeeItemId, s.Quantity }
            )
            .GroupBy(static x => x.FeeItemId)
            .Select(static g => new { FeeItemId = g.Key, Total = g.Sum(x => x.Quantity) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(static r => r.FeeItemId, static r => r.Total);
    }

    public Task<int> CountDiscountedRegistrantsAsync(
        RegistrationInstanceId instanceId,
        string? discountCode,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(discountCode))
        {
            return Task.FromResult(0);
        }

        var normalized = discountCode.Trim().ToUpperInvariant();
        return dbContext
            .RegistrationRegistrants.Where(r =>
                r.InstanceId == instanceId && r.Status == RegistrantStatus.Confirmed
            )
            .Join(
                dbContext.Registrations.Where(reg => reg.AppliedDiscountCode == normalized),
                static r => r.RegistrationId,
                static reg => reg.Id,
                static (r, _) => r.Id
            )
            .CountAsync(cancellationToken);
    }
}
