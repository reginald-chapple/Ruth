using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.Commerce.Slices.Events;

internal sealed class ServiceBookingWalletPassSyncConsumer(
    WalletPassCommerceSyncCollaborator walletPassSync,
    IClock clock
) : IIntegrationEventConsumer<ServiceBookingConfirmedIntegrationEvent>
{
    public async Task ConsumeAsync(
        ServiceBookingConfirmedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (integrationEvent.CustomerPersonId is { } personId && personId != Guid.Empty)
        {
            await walletPassSync.SyncUpcomingBookingForPersonAsync(
                personId,
                clock.UtcNow,
                cancellationToken
            );
        }
    }
}
