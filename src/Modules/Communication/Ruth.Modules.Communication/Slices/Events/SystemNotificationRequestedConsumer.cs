using Ruth.Modules.Communication.Contracts.Events;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class SystemNotificationRequestedConsumer(ICommandDispatcher commandDispatcher)
    : IIntegrationEventConsumer<SystemNotificationRequestedIntegrationEvent>
{
    public async Task ConsumeAsync(
        SystemNotificationRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _ = await commandDispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(
            new SendSystemCommunicationCommand(
                integrationEvent.SystemCommunicationKey,
                integrationEvent.RecipientEmail,
                integrationEvent.RecipientName,
                integrationEvent.RecipientPhoneNumber,
                integrationEvent.RecipientPersonId,
                integrationEvent.MergeFields,
                integrationEvent.TriggerModule ?? "Outbox.SystemNotificationRequested"
            ),
            cancellationToken
        );
    }
}
