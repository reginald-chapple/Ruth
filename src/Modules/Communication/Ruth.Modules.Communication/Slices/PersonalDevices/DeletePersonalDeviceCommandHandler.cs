using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PersonalDevices;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Communication.Slices.PersonalDevices;

internal sealed class DeletePersonalDeviceCommandHandler(CommunicationDbContext dbContext)
    : ICommandHandler<DeletePersonalDeviceCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        DeletePersonalDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = PersonalDeviceId.From(command.PersonalDeviceId);
        var device = await dbContext.PersonalDevices.FirstOrDefaultAsync(
            d => d.Id == id,
            cancellationToken
        );
        if (device is null)
        {
            return CommunicationDomainErrors.PersonalDeviceNotFound;
        }

        _ = dbContext.PersonalDevices.Remove(device);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
