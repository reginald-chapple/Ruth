using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class UnregisterPassKitDeviceCommandHandler(CheckInDbContext dbContext)
    : ICommandHandler<UnregisterPassKitDeviceCommand>
{
    public async Task<Result> HandleAsync(
        UnregisterPassKitDeviceCommand command,
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
            return WalletPassErrors.PassNotFound;
        }

        if (!pass.VerifyAppleAuthToken(command.ApplePassAuthToken))
        {
            return WalletPassErrors.PassKitUnauthorized;
        }

        var deviceId = command.DeviceLibraryIdentifier?.Trim() ?? string.Empty;
        var existing = await dbContext.WalletPassDeviceRegistrations.FirstOrDefaultAsync(
            r =>
                r.DeviceLibraryIdentifier == deviceId
                && r.PassTypeIdentifier == pass.PassTypeIdentifier
                && r.SerialNumber == pass.SerialNumber,
            cancellationToken
        );
        if (existing is null)
        {
            return WalletPassErrors.PassNotFound;
        }

        _ = dbContext.WalletPassDeviceRegistrations.Remove(existing);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
