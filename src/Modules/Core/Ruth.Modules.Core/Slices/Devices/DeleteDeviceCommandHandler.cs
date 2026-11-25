using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Core.Contracts.Devices;
using Ruth.Modules.Core.Domain;
using Ruth.Modules.Core.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Slices.Devices;

internal sealed class DeleteDeviceCommandHandler(CoreDbContext dbContext)
    : ICommandHandler<DeleteDeviceCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeleteDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        var id = DeviceId.From(command.DeviceId);
        var device = await dbContext.Devices.FirstOrDefaultAsync(
            d => d.Id == id,
            cancellationToken
        );
        if (device is null)
        {
            return CoreLocationErrors.DeviceNotFound;
        }

        _ = dbContext.Devices.Remove(device);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(value: true);
    }
}
