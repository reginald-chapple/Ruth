using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Devices;

internal sealed class UpdateDeviceCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<UpdateDeviceCommand, DeviceResponse>
{
    public async Task<Result<DeviceResponse>> HandleAsync(
        UpdateDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = DeviceId.From(command.DeviceId);
        var device = await dbContext
            .Devices.Include(static d => d.Locations)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (device is null)
        {
            return CoreLocationErrors.DeviceNotFound;
        }

        var updated = device.Update(DeviceMapper.ToInput(command));
        if (updated.IsFailure)
        {
            return Result.Failure<DeviceResponse>(updated.Errors);
        }

        var duplicateName = await dbContext.Devices.AnyAsync(
            d => d.Id != id && d.NormalizedName == device.NormalizedName,
            cancellationToken
        );
        if (duplicateName)
        {
            return CoreLocationErrors.DeviceDuplicateName;
        }

        if (!string.IsNullOrWhiteSpace(device.NormalizedIpAddress))
        {
            var typeId = device.DeviceTypeValueId;
            var ip = device.NormalizedIpAddress;
            var duplicateIp = await dbContext.Devices.AnyAsync(
                d => d.Id != id && d.DeviceTypeValueId == typeId && d.NormalizedIpAddress == ip,
                cancellationToken
            );
            if (duplicateIp)
            {
                return CoreLocationErrors.DeviceDuplicateIp;
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(DeviceMapper.ToResponse(device));
    }
}
