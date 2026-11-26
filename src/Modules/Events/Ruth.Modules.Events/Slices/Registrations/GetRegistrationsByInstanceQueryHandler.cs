using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Ruth.Modules.Events.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Slices.Registrations;

internal sealed class GetRegistrationsByInstanceQueryHandler(EventsDbContext dbContext)
    : IQueryHandler<GetRegistrationsByInstanceQuery, IReadOnlyList<RegistrationResponse>>
{
    public async Task<Result<IReadOnlyList<RegistrationResponse>>> HandleAsync(
        GetRegistrationsByInstanceQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var instanceId = RegistrationInstanceId.From(query.InstanceId);
        var items = await dbContext
            .Registrations.AsNoTracking()
            .Include(static r => r.Registrants)
                .ThenInclude(static reg => reg.FeeSelections)
            .Include(static r => r.Payments)
            .AsSplitQuery()
            .Where(r => r.InstanceId == instanceId)
            .OrderByDescending(static r => r.CreatedOnUtc)
            .ToListAsync(cancellationToken);

        return items.Select(RegistrationMapper.MapRegistration).ToArray();
    }
}
