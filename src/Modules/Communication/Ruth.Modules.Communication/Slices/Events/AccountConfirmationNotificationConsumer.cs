using System.Globalization;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class AccountConfirmationNotificationConsumer(ICommandDispatcher commandDispatcher)
    : IIntegrationEventConsumer<AccountConfirmationRequestedIntegrationEvent>
{
    public async Task ConsumeAsync(
        AccountConfirmationRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = integrationEvent.UserName,
            ["ConfirmationToken"] = integrationEvent.ConfirmationToken,
            ["ExpiresOnUtc"] = integrationEvent.ExpiresOnUtc.ToString(
                "u",
                CultureInfo.InvariantCulture
            ),
        };

        _ = await commandDispatcher.SendAsync<
            SendSystemCommunicationCommand,
            SystemCommunicationSendResultResponse
        >(
            new SendSystemCommunicationCommand(
                "identity.account-confirmation",
                RecipientEmail: integrationEvent.Email,
                RecipientName: integrationEvent.UserName,
                MergeFields: merge,
                TriggerSource: "Identity.AccountConfirmationRequested"
            ),
            cancellationToken
        );
    }
}
