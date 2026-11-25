using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Devices;

internal sealed class CreateDeviceCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<CreateDeviceCommand, DeviceResponse>
{
    public async Task<Result<DeviceResponse>> HandleAsync(
        CreateDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var created = Device.Create(DeviceMapper.ToInput(command));
        if (created.IsFailure)
        {
            return Result.Failure<DeviceResponse>(created.Errors);
        }

        var duplicateName = await dbContext.Devices.AnyAsync(
            d => d.NormalizedName == created.Value.NormalizedName,
            cancellationToken
        );
        if (duplicateName)
        {
            return CoreLocationErrors.DeviceDuplicateName;
        }

        if (!string.IsNullOrWhiteSpace(created.Value.NormalizedIpAddress))
        {
            var typeId = created.Value.DeviceTypeValueId;
            var ip = created.Value.NormalizedIpAddress;
            var duplicateIp = await dbContext.Devices.AnyAsync(
                d => d.DeviceTypeValueId == typeId && d.NormalizedIpAddress == ip,
                cancellationToken
            );
            if (duplicateIp)
            {
                return CoreLocationErrors.DeviceDuplicateIp;
            }
        }

        _ = dbContext.Devices.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(DeviceMapper.ToResponse(created.Value));
    }
}
