using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class RegisterPassKitDeviceCommandHandler(CheckInDbContext dbContext, IClock clock)
    : ICommandHandler<RegisterPassKitDeviceCommand, bool>
{
    public async Task<Result<bool>> HandleAsync(
        RegisterPassKitDeviceCommand command,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(command);

        var passType = command.PassTypeIdentifier?.Trim().ToLowerInvariant() ?? string.Empty;
        var serial = command.SerialNumber?.Trim() ?? string.Empty;
        var pass = await dbContext
            .IssuedWalletPasses.AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.PassTypeIdentifier == passType && p.SerialNumber == serial,
                cancellationToken
            );
        if (pass is null)
        {
            return Result.Failure<bool>(WalletPassErrors.PassNotFound);
        }

        if (!pass.VerifyAppleAuthToken(command.ApplePassAuthToken))
        {
            return Result.Failure<bool>(WalletPassErrors.PassKitUnauthorized);
        }

        var deviceId = command.DeviceLibraryIdentifier?.Trim() ?? string.Empty;
        var existing = await dbContext.WalletPassDeviceRegistrations.FirstOrDefaultAsync(
            r =>
                r.DeviceLibraryIdentifier == deviceId
                && r.PassTypeIdentifier == pass.PassTypeIdentifier
                && r.SerialNumber == pass.SerialNumber,
            cancellationToken
        );

        var nowUtc = clock.UtcNow;
        if (existing is not null)
        {
            var refreshed = existing.UpdatePushToken(command.PushToken, nowUtc);
            if (refreshed.IsFailure)
            {
                return Result.Failure<bool>(refreshed.Errors);
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        var created = WalletPassDeviceRegistration.Create(
            pass,
            deviceId,
            command.PushToken,
            nowUtc
        );
        if (created.IsFailure)
        {
            return Result.Failure<bool>(created.Errors);
        }

        _ = dbContext.WalletPassDeviceRegistrations.Add(created.Value);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
