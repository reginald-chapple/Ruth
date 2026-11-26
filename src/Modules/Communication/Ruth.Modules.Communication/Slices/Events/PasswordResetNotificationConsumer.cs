using System.Globalization;
using Ruth.Modules.Communication.Contracts.SystemCommunications;
using Ruth.Modules.Identity.Contracts.Authentication;
using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Slices.Events;

internal sealed class PasswordResetNotificationConsumer(ICommandDispatcher commandDispatcher)
    : IIntegrationEventConsumer<PasswordResetRequestedIntegrationEvent>
{
    public async Task ConsumeAsync(
        PasswordResetRequestedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var merge = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["UserName"] = integrationEvent.UserName,
            ["ResetToken"] = integrationEvent.ResetToken,
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
                "identity.password-reset",
                RecipientEmail: integrationEvent.Email,
                RecipientName: integrationEvent.UserName,
                MergeFields: merge,
                TriggerSource: "Identity.PasswordResetRequested"
            ),
            cancellationToken
        );
    }
}
