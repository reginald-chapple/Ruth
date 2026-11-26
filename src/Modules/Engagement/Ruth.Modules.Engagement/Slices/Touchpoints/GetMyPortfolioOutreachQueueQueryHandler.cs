using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Engagement.Contracts.Touchpoints;
using Ruth.Modules.Engagement.Domain.Touchpoints;
using Ruth.Modules.Engagement.Infrastructure;
using Ruth.Modules.Engagement.Slices.Pipelines;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Engagement.Slices.Touchpoints;

internal sealed class GetMyPortfolioOutreachQueueQueryHandler(
    EngagementDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    IClock clock
) : IQueryHandler<GetMyPortfolioOutreachQueueQuery, PortfolioOutreachQueueResponse>
{
    public async Task<Result<PortfolioOutreachQueueResponse>> HandleAsync(
        GetMyPortfolioOutreachQueueQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);
        var contacts = await LoadContactsAsync(query, cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        foreach (var c in contacts)
        {
            c.RefreshScheduleAndHealth(today);
        }

        var contactIds = contacts.Select(static c => c.Id.Value).ToHashSet();
        var touchpoints =
            contactIds.Count == 0
                ? []
                : await dbContext
                    .Touchpoints.AsNoTracking()
                    .Where(t => contactIds.Contains(t.ConstituentContactId))
                    .ToListAsync(cancellationToken);

        var people = await PipelineProjectionMapper.LoadPeopleAsync(
            queryDispatcher,
            contacts.Select(static c => c.ConstituentPersonId),
            cancellationToken
        );

        return BuildQueueResponse(query.OwnerPersonId ?? Guid.Empty, contacts, touchpoints, people);
    }

    private async Task<List<ConstituentContact>> LoadContactsAsync(
        GetMyPortfolioOutreachQueueQuery query,
        CancellationToken cancellationToken
    )
    {
        var cq = dbContext.ConstituentContacts.AsNoTracking().AsQueryable();
        if (query.OwnerPersonId is { } oid && oid != Guid.Empty)
        {
            cq = cq.Where(c => c.OwnerPersonId == oid);
        }

        if (query.BranchId is { } bid && bid != Guid.Empty)
        {
            cq = cq.Where(c => c.BranchId == bid);
        }

        return await cq.ToListAsync(cancellationToken);
    }

    private static PortfolioOutreachQueueResponse BuildQueueResponse(
        Guid ownerPersonId,
        List<ConstituentContact> contacts,
        List<Touchpoint> touchpoints,
        IReadOnlyDictionary<Guid, (string Name, string? Email)> people
    )
    {
        var roleMap = contacts.ToDictionary(static c => c.Id.Value, static c => c.PortfolioRole);
        var mappedContacts = contacts
            .OrderBy(static c => c.HealthScore)
            .Select(c => TouchpointProjectionMapper.ToResponse(c, people))
            .ToArray();

        var queue = touchpoints
            .Where(static t => t.Status is TouchpointStatus.Pending or TouchpointStatus.Snoozed)
            .OrderBy(static t => t.ScheduledDate)
            .Select(t =>
                TouchpointProjectionMapper.ToResponse(
                    t,
                    roleMap.GetValueOrDefault(t.ConstituentContactId, "Steward"),
                    people
                )
            )
            .ToArray();

        var recentCompleted = touchpoints
            .Where(static t => t.Status == TouchpointStatus.Completed)
            .OrderByDescending(static t => t.CompletedAtUtc)
            .Take(25)
            .Select(t =>
                TouchpointProjectionMapper.ToResponse(
                    t,
                    roleMap.GetValueOrDefault(t.ConstituentContactId, "Steward"),
                    people
                )
            )
            .ToArray();

        var active = mappedContacts.Where(static c => c.IsActive).ToArray();
        var avgHealth =
            active.Length == 0
                ? 100
                : (int)
                    Math.Round(
                        active.Average(static c => c.HealthScore),
                        MidpointRounding.AwayFromZero
                    );

        return new PortfolioOutreachQueueResponse(
            ownerPersonId,
            active.Length,
            active.Count(static c => c.HealthStatus == ConstituentHealthStatus.Healthy),
            active.Count(static c =>
                c.HealthStatus is ConstituentHealthStatus.DueSoon or ConstituentHealthStatus.Overdue
            ),
            active.Count(static c => c.HealthStatus == ConstituentHealthStatus.CriticalLapse),
            avgHealth,
            queue,
            recentCompleted,
            mappedContacts
        );
    }
}
