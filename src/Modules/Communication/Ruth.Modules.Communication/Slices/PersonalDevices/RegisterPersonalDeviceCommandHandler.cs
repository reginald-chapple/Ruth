using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Communication.Contracts.PersonalDevices;
using Ruth.Modules.Communication.Domain.PersonalDevices;
using Ruth.Modules.Communication.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Communication.Slices.PersonalDevices;

internal sealed class RegisterPersonalDeviceCommandHandler(
    CommunicationDbContext dbContext,
    IClock clock
) : ICommandHandler<RegisterPersonalDeviceCommand, PersonalDeviceResponse>
{
    public async Task<Result<PersonalDeviceResponse>> HandleAsync(
        RegisterPersonalDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var validToken = PersonalDevice.ValidateToken(command.PushRegistrationToken);
        if (validToken.IsFailure)
        {
            return Result.Failure<PersonalDeviceResponse>(validToken.Errors);
        }

        var now = clock.UtcNow;
        var existing = await dbContext.PersonalDevices.FirstOrDefaultAsync(
            d => d.PushRegistrationToken == validToken.Value,
            cancellationToken
        );
        if (existing is not null)
        {
            existing.RefreshRegistration(
                command.Platform,
                command.PersonId,
                command.DeviceName,
                command.DeviceVersion,
                command.NotificationsEnabled,
                now
            );
            existing.SetModified(now);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return PersonalDeviceMapper.ToResponse(existing);
        }

        var registered = PersonalDevice.Register(
            command.Platform,
            validToken.Value,
            command.PersonId,
            command.DeviceName,
            command.DeviceVersion,
            command.NotificationsEnabled,
            now
        );
        if (registered.IsFailure)
        {
            return Result.Failure<PersonalDeviceResponse>(registered.Errors);
        }

        registered.Value.SetCreated(now);
        _ = dbContext.PersonalDevices.Add(registered.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return PersonalDeviceMapper.ToResponse(registered.Value);
    }
}
