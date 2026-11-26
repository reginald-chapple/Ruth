using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Calendars;
using Ruth.Modules.Events.Domain.Calendars;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Events.Infrastructure;

internal sealed class EventsTenantSeedContributor(EventsDbContext dbContext)
    : ITenantSeedContributor
{
    public int Order => 70;

    public async Task SeedTenantAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default
    )
    {
        if (await dbContext.EventCalendars.AnyAsync(cancellationToken))
        {
            return;
        }

        EventCalendarSpec[] defaults =
        [
            new(
                Name: "Public Events Calendar",
                Slug: "public-events",
                Description: "Public-facing community, member, and program events.",
                ColorHex: "#0F6CBD",
                IsPublic: true,
                IsIndexEnabled: true,
                IsActive: true
            ),
            new(
                Name: "Internal Operations Calendar",
                Slug: "internal-operations",
                Description: "Staff, volunteer, and facility coordination calendar.",
                ColorHex: "#498205",
                IsPublic: false,
                IsIndexEnabled: false,
                IsActive: true
            ),
        ];

        foreach (var spec in defaults)
        {
            var created = EventCalendar.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.EventCalendars.Add(created.Value);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }
}
