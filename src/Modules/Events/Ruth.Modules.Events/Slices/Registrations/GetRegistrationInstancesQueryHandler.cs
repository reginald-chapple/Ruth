using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class GetRegistrationInstancesQueryHandler(
    EventsDbContext dbContext,
    RegistrationInventoryLoader loader,
    TimeProvider timeProvider
) : IQueryHandler<GetRegistrationInstancesQuery, IReadOnlyList<RegistrationInstanceResponse>>
{
    public async Task<Result<IReadOnlyList<RegistrationInstanceResponse>>> HandleAsync(
        GetRegistrationInstancesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = dbContext.RegistrationInstances.AsNoTracking().AsQueryable();
        if (query.TemplateId is { } tid && tid != Guid.Empty)
        {
            var templateId = RegistrationTemplateId.From(tid);
            q = q.Where(i => i.TemplateId == templateId);
        }

        if (query.BranchId is { } bid && bid != Guid.Empty)
        {
            q = q.Where(i => i.BranchId == bid);
        }

        if (query.ActiveOnly)
        {
            q = q.Where(static i => i.IsActive);
        }

        var instances = await q.OrderByDescending(static i => i.CreatedOnUtc)
            .ToListAsync(cancellationToken);
        var templateIds = instances.Select(static i => i.TemplateId).Distinct().ToArray();
        var templates = await dbContext
            .RegistrationTemplates.AsNoTracking()
            .Where(t => templateIds.Contains(t.Id))
            .ToDictionaryAsync(static t => t.Id, cancellationToken);

        var nowUtc = timeProvider.GetUtcNow();
        var responses = new List<RegistrationInstanceResponse>(instances.Count);
        foreach (var inst in instances)
        {
            if (templates.TryGetValue(inst.TemplateId, out var tmpl))
            {
                var (confirmed, held, waitlisted, _) = await loader.LoadSeatSnapshotAsync(
                    inst.Id,
                    nowUtc,
                    excludeSessionToken: null,
                    cancellationToken
                );
                responses.Add(
                    RegistrationMapper.MapInstance(inst, tmpl, confirmed, held, waitlisted, nowUtc)
                );
            }
        }

        return responses;
    }
}
