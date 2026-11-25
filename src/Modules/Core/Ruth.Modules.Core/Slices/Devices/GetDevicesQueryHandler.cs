using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Devices;

internal sealed class GetDevicesQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetDevicesQuery, IReadOnlyList<DeviceResponse>>
{
    public async Task<Result<IReadOnlyList<DeviceResponse>>> HandleAsync(
        GetDevicesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var baseQuery = dbContext
            .Devices.AsNoTracking()
            .Include(static d => d.Locations)
            .AsQueryable();
        if (query.DeviceTypeValueId.HasValue)
        {
            var typeId = query.DeviceTypeValueId.Value;
            baseQuery = baseQuery.Where(d => d.DeviceTypeValueId == typeId);
        }

        if (query.ActiveOnly)
        {
            baseQuery = baseQuery.Where(static d => d.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim().ToUpperInvariant();
            baseQuery = baseQuery.Where(d =>
                d.NormalizedName.Contains(term)
                || (d.NormalizedIpAddress != null && d.NormalizedIpAddress.Contains(term))
            );
        }

        var devices = await baseQuery.OrderBy(static d => d.Name).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<DeviceResponse>>([
            .. devices.Select(DeviceMapper.ToResponse),
        ]);
    }
}
