using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Domain;
using Ruth.Modules.Communication.Domain.PersonalDevices;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.PersonalDevices;

internal sealed class SetPersonalDeviceNotificationsCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<SetPersonalDeviceNotificationsCommand, PersonalDeviceResponse>
{
    public async Task<Result<PersonalDeviceResponse>> HandleAsync(
        SetPersonalDeviceNotificationsCommand command,
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

        device.SetNotificationPreferences(command.NotificationsEnabled, command.IsActive);
        device.SetModified(clock.UtcNow);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PersonalDeviceMapper.ToResponse(device);
    }
}
