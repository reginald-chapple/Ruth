using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.PersonalDevices;

internal sealed class GetPersonalDevicesQueryHandler(CommunicationDbContext dbContext)
    : IQueryHandler<GetPersonalDevicesQuery, IReadOnlyList<PersonalDeviceResponse>>
{
    public async Task<Result<IReadOnlyList<PersonalDeviceResponse>>> HandleAsync(
        GetPersonalDevicesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var dbQuery = dbContext.PersonalDevices.AsNoTracking();
        if (query.PersonId.HasValue)
        {
            dbQuery = dbQuery.Where(d => d.PersonId == query.PersonId.Value);
        }

        if (query.ActiveOnly)
        {
            dbQuery = dbQuery.Where(static d => d.IsActive && d.NotificationsEnabled);
        }

        var list = await dbQuery
            .OrderByDescending(static d => d.LastSeenOnUtc)
            .ToListAsync(cancellationToken);

        IReadOnlyList<PersonalDeviceResponse> items =
        [
            .. list.Select(PersonalDeviceMapper.ToResponse),
        ];
        return Result.Success(items);
    }
}
