using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.CheckIn.Domain.WalletPasses;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.WalletPasses;

internal sealed class WalletPassPushDispatchCollaborator(
    CheckInDbContext dbContext,
    IWalletPassPushNotifier pushNotifier,
    IClock clock
)
{
    public async Task<int> DispatchPushUpdatesAndCountDevicesAsync(
        IssuedWalletPass pass,
        CancellationToken cancellationToken
    )
    {
        var registrations = await dbContext
            .WalletPassDeviceRegistrations.Where(r => r.IssuedPassId == pass.Id)
            .ToListAsync(cancellationToken);
        if (registrations.Count == 0)
        {
            return 0;
        }

        var tokens = registrations
            .Select(static r => r.PushToken)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        _ = await pushNotifier.NotifyPassUpdatedAsync(
            pass.PassTypeIdentifier,
            pass.SerialNumber,
            tokens,
            cancellationToken
        );

        var nowUtc = clock.UtcNow.ToUniversalTime();
        foreach (var reg in registrations)
        {
            reg.MarkNotified(nowUtc);
        }

        return registrations.Count;
    }
}
