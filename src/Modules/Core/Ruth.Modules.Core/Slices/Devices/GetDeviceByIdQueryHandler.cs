using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Devices;

internal sealed class GetDeviceByIdQueryHandler(CoreDbContext dbContext)
    : IQueryHandler<GetDeviceByIdQuery, DeviceResponse>
{
    public async Task<Result<DeviceResponse>> HandleAsync(
        GetDeviceByIdQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var id = DeviceId.From(query.DeviceId);
        var device = await dbContext
            .Devices.AsNoTracking()
            .Include(static d => d.Locations)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return device is null
            ? CoreLocationErrors.DeviceNotFound
            : Result.Success(DeviceMapper.ToResponse(device));
    }
}
